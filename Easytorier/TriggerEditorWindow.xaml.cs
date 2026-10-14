using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls;
using Easytorier.Trigger;

namespace Easytorier;

public sealed partial class TriggerEditorWindow : Window
{
    private sealed class EntryChoice
    {
        public string Display { get; }
        public EntryDef Entry { get; }
        public EntryChoice(EntryDef entry) { Entry = entry; Display = entry.title; }
    }
    private sealed class NodeChoice<T>
    {
        public string Display { get; }
        public T Value { get; }
        public NodeChoice(string display, T value) { Display = display; Value = value; }
    }
    public sealed class ParamRow
    {
        public string Attr { get; set; } = "";
        public string Value { get; set; } = "";
    }
    public sealed class VariableRow
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public bool Locked { get; set; }
    }

    private TriggerData _data = TriggerData.CreateDefault();
    private TLoop? _selectedLoop;
    private bool _refreshing;

    public string TriggerXml { get; private set; } = "";

    public TriggerEditorWindow(string triggerContentXml, string objectName)
    {
        InitializeComponent();
        ObjectLabel.Text = $"Editing {objectName}: loop events, conditions, actions, and variables.";
        EventCatalogBox.ItemsSource = TriggerCatalog.Events.Select(e => new EntryChoice(e)).ToList();
        ActionCatalogBox.ItemsSource = TriggerCatalog.Actions.Select(a => new EntryChoice(a)).ToList();
        EventCatalogBox.SelectedIndex = ActionCatalogBox.SelectedIndex = 0;
        ConditionKindBox.ItemsSource = new[] { "Equal", "Greater", "Less", "And group", "Or group" };
        ConditionKindBox.SelectedIndex = 0;
        CompareBox.ItemsSource = new[] { "Equal", "Greater", "Less" };
        CompareBox.SelectedIndex = 0;
        TemplateBox.ItemsSource = LoadTemplateNames();
        _data = TriggerData.Parse(ExtractInner(triggerContentXml));
        RefreshAll();
    }

    private static string[] LoadTemplateNames()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "TriggerTemplates.xml");
            if (!File.Exists(path)) return Array.Empty<string>();
            return XDocument.Load(path).Descendants("Template")
                .Select(node => (string?)node.Attribute("Name"))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    private static string ExtractInner(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return "";
        var doc = new XmlDocument { XmlResolver = null };
        doc.LoadXml(xml);
        return doc.DocumentElement is { Name: "Content" or "Root" } root ? root.InnerXml : xml;
    }

    private static string[] BuildRows(TriggerData data) => Array.Empty<string>();

    private void RefreshAll()
    {
        _data.EnsureLockedVars();
        _refreshing = true;
        try
        {
            LoopList.ItemsSource = null;
            LoopList.ItemsSource = _data.loops;
            LoopList.DisplayMemberPath = nameof(TLoop.Display);
            LoopList.SelectedIndex = _data.loops.Count == 0 ? -1 : 0;
            RefreshVariables();
            XmlEditor.Text = _data.ToXml();
        }
        finally { _refreshing = false; }
        if (_data.loops.Count > 0) RefreshLoopContents(_data.loops[0]);
    }

    private void RefreshLoopContents(TLoop? loop)
    {
        _selectedLoop = loop;
        _refreshing = true;
        try
        {
            EventList.ItemsSource = loop?.events.Select((item, index) =>
                new NodeChoice<TEvent>(DescribeEvent(item), item)).ToList();
            ConditionList.ItemsSource = loop?.conds.Select(item =>
                new NodeChoice<TCond>(DescribeCondition(item), item)).ToList();
            ActionList.ItemsSource = loop?.acts.Select(item =>
                new NodeChoice<TAct>(DescribeAction(item), item)).ToList();
            EventParamsGrid.ItemsSource = null;
            ActionParamsGrid.ItemsSource = null;
            CondLeftBox.Text = CondRightBox.Text = "";
            CondNotBox.IsChecked = false;
        }
        finally { _refreshing = false; }
    }

    private static string DescribeEvent(TEvent item)
    {
        if (item.kind == EvKind.Raw) return "Unrecognized event (preserved XML)";
        if (item.kind == EvKind.Block) return $"Template event: {item.template}";
        return TriggerCatalog.FindEvent(item.id)?.title ?? item.id;
    }
    private static string DescribeCondition(TCond item) => item.kind switch
    {
        CondKind.Compare => $"{item.cmp}: {item.a.ToXml()}  /  {item.b.ToXml()}",
        CondKind.Group => $"{item.op} group ({item.children.Count})",
        CondKind.Select => "Select condition",
        CondKind.Block => $"Template condition: {item.template}",
        _ => "Unrecognized condition (preserved XML)"
    };
    private static string DescribeAction(TAct item)
    {
        if (item.kind == ActKind.Raw) return "Unrecognized action (preserved XML)";
        if (item.kind == ActKind.Block) return $"Template action: {item.template}";
        if (item.kind == ActKind.Choose) return $"Choose group ({item.children.Count})";
        return TriggerCatalog.FindAction(item.id)?.title ?? item.id;
    }

    private void RefreshVariables()
    {
        VariableGrid.ItemsSource = null;
        VariableGrid.ItemsSource = new ObservableCollection<VariableRow>(_data.vars.Select(v =>
            new VariableRow { Name = v.name, Value = v.value, Locked = v.locked }));
    }

    private void OnLoopSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing) return;
        RefreshLoopContents(LoopList.SelectedItem as TLoop);
    }

    private void OnAddLoop(object sender, RoutedEventArgs e)
    {
        var loop = new TLoop { name = "Loop" };
        _data.loops.Add(loop);
        RefreshAll();
        LoopList.SelectedItem = loop;
    }
    private void OnRemoveLoop(object sender, RoutedEventArgs e)
    {
        if (LoopList.SelectedItem is not TLoop loop) return;
        _data.loops.Remove(loop);
        RefreshAll();
    }
    private void OnAddTemplate(object sender, RoutedEventArgs e)
    {
        var name = TemplateBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(name)) return;
        _data.loops.Add(new TLoop { kind = LoopKind.FullTemplate, template = name });
        RefreshAll();
        LoopList.SelectedIndex = LoopList.Items.Count - 1;
    }

    private void OnAddEvent(object sender, RoutedEventArgs e)
    {
        if (_selectedLoop is null || EventCatalogBox.SelectedItem is not EntryChoice choice) return;
        var ev = new TEvent { id = choice.Entry.id };
        foreach (var parameter in choice.Entry.ps)
            ev.ps.Add(new TParam(parameter.attr, TValue.Parse(parameter.def)));
        _selectedLoop.events.Add(ev);
        RefreshLoopContents(_selectedLoop);
        EventList.SelectedIndex = EventList.Items.Count - 1;
        XmlEditor.Text = _data.ToXml();
    }
    private void OnRemoveEvent(object sender, RoutedEventArgs e)
    {
        if (_selectedLoop is null || EventList.SelectedItem is not NodeChoice<TEvent> item) return;
        _selectedLoop.events.Remove(item.Value);
        RefreshLoopContents(_selectedLoop);
        XmlEditor.Text = _data.ToXml();
    }
    private void OnEventSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || EventList.SelectedItem is not NodeChoice<TEvent> choice) return;
        var item = choice.Value;
        var def = item.kind == EvKind.Event ? TriggerCatalog.FindEvent(item.id) : null;
        if (def is not null)
        {
            foreach (var parameter in def.ps)
                if (!item.ps.Any(p => p.attr == parameter.attr))
                    item.ps.Add(new TParam(parameter.attr, TValue.Parse(parameter.def)));
        }
        EventParamsGrid.ItemsSource = item.kind == EvKind.Event
            ? item.ps.Select(p => new ParamRow { Attr = p.attr, Value = p.v.ToXml() }).ToList()
            : null;
    }
    private void OnApplyEvent(object sender, RoutedEventArgs e)
    {
        if (EventList.SelectedItem is not NodeChoice<TEvent> choice || EventParamsGrid.ItemsSource is not System.Collections.IEnumerable rows) return;
        foreach (var row in rows.OfType<ParamRow>())
            SetParam(choice.Value.ps, row.Attr, row.Value);
        RefreshLoopContents(_selectedLoop);
        XmlEditor.Text = _data.ToXml();
    }

    private void OnAddCondition(object sender, RoutedEventArgs e)
    {
        if (_selectedLoop is null || ConditionKindBox.SelectedItem is not string kind) return;
        var condition = kind switch
        {
            "And group" => new TCond { kind = CondKind.Group, op = "And" },
            "Or group" => new TCond { kind = CondKind.Group, op = "Or" },
            _ => new TCond { kind = CondKind.Compare, cmp = Enum.TryParse<Cmp>(kind, out var cmp) ? cmp : Cmp.Equal }
        };
        _selectedLoop.conds.Add(condition);
        RefreshLoopContents(_selectedLoop);
        ConditionList.SelectedIndex = ConditionList.Items.Count - 1;
        XmlEditor.Text = _data.ToXml();
    }
    private void OnRemoveCondition(object sender, RoutedEventArgs e)
    {
        if (_selectedLoop is null || ConditionList.SelectedItem is not NodeChoice<TCond> item) return;
        _selectedLoop.conds.Remove(item.Value);
        RefreshLoopContents(_selectedLoop);
        XmlEditor.Text = _data.ToXml();
    }
    private void OnConditionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || ConditionList.SelectedItem is not NodeChoice<TCond> choice) return;
        var condition = choice.Value;
        CompareBox.SelectedItem = condition.cmp.ToString();
        CondLeftBox.Text = condition.kind == CondKind.Compare ? condition.a.ToXml() : "";
        CondRightBox.Text = condition.kind == CondKind.Compare ? condition.b.ToXml() : "";
        CondNotBox.IsChecked = condition.not;
    }
    private void OnApplyCondition(object sender, RoutedEventArgs e)
    {
        if (ConditionList.SelectedItem is not NodeChoice<TCond> choice) return;
        var condition = choice.Value;
        if (condition.kind == CondKind.Compare)
        {
            if (CompareBox.SelectedItem is string cmp && Enum.TryParse<Cmp>(cmp, out var parsed)) condition.cmp = parsed;
            SetValue(condition.a, CondLeftBox.Text);
            SetValue(condition.b, CondRightBox.Text);
            condition.not = CondNotBox.IsChecked == true;
        }
        RefreshLoopContents(_selectedLoop);
        XmlEditor.Text = _data.ToXml();
    }

    private void OnAddAction(object sender, RoutedEventArgs e)
    {
        if (_selectedLoop is null || ActionCatalogBox.SelectedItem is not EntryChoice choice) return;
        var action = new TAct { id = choice.Entry.id };
        foreach (var parameter in choice.Entry.ps)
            action.ps.Add(new TParam(parameter.attr, TValue.Parse(parameter.def)));
        _selectedLoop.acts.Add(action);
        RefreshLoopContents(_selectedLoop);
        ActionList.SelectedIndex = ActionList.Items.Count - 1;
        XmlEditor.Text = _data.ToXml();
    }
    private void OnAddChoose(object sender, RoutedEventArgs e)
    {
        if (_selectedLoop is null) return;
        _selectedLoop.acts.Add(new TAct { kind = ActKind.Choose, order = "Random", set = 1 });
        RefreshLoopContents(_selectedLoop);
        ActionList.SelectedIndex = ActionList.Items.Count - 1;
        XmlEditor.Text = _data.ToXml();
    }
    private void OnRemoveAction(object sender, RoutedEventArgs e)
    {
        if (_selectedLoop is null || ActionList.SelectedItem is not NodeChoice<TAct> item) return;
        _selectedLoop.acts.Remove(item.Value);
        RefreshLoopContents(_selectedLoop);
        XmlEditor.Text = _data.ToXml();
    }
    private void OnActionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || ActionList.SelectedItem is not NodeChoice<TAct> choice) return;
        var item = choice.Value;
        var def = item.kind == ActKind.Action ? TriggerCatalog.FindAction(item.id) : null;
        if (def is not null)
        {
            foreach (var parameter in def.ps)
                if (!item.ps.Any(p => p.attr == parameter.attr))
                    item.ps.Add(new TParam(parameter.attr, TValue.Parse(parameter.def)));
        }
        ActionParamsGrid.ItemsSource = item.kind == ActKind.Action
            ? item.ps.Select(p => new ParamRow { Attr = p.attr, Value = p.v.ToXml() }).ToList()
            : null;
    }
    private void OnApplyAction(object sender, RoutedEventArgs e)
    {
        if (ActionList.SelectedItem is not NodeChoice<TAct> choice || ActionParamsGrid.ItemsSource is not System.Collections.IEnumerable rows) return;
        foreach (var row in rows.OfType<ParamRow>())
            SetParam(choice.Value.ps, row.Attr, row.Value);
        RefreshLoopContents(_selectedLoop);
        XmlEditor.Text = _data.ToXml();
    }

    private void OnAddVariable(object sender, RoutedEventArgs e)
    {
        _data.vars.Add(new TVar("NewVariable", "0"));
        RefreshVariables();
        VariableGrid.SelectedIndex = VariableGrid.Items.Count - 1;
        XmlEditor.Text = _data.ToXml();
    }
    private void OnRemoveVariable(object sender, RoutedEventArgs e)
    {
        if (VariableGrid.SelectedItem is not VariableRow row || row.Locked) return;
        var variable = _data.vars.FirstOrDefault(v => !v.locked && v.name == row.Name);
        if (variable is not null) _data.vars.Remove(variable);
        RefreshVariables();
        XmlEditor.Text = _data.ToXml();
    }
    private void CommitVariables()
    {
        if (VariableGrid.ItemsSource is not ObservableCollection<VariableRow> rows) return;
        var locked = _data.vars.Where(v => v.locked).ToList();
        var editable = rows.Where(v => !v.Locked && !string.IsNullOrWhiteSpace(v.Name))
            .Select(v => new TVar(v.Name.Trim(), v.Value ?? "0")).ToList();
        _data.vars = locked.Concat(editable).ToList();
        _data.EnsureLockedVars();
    }

    private void OnApplyXml(object sender, RoutedEventArgs e)
    {
        try
        {
            var inner = ExtractInner(XmlEditor.Text);
            var check = new XmlDocument { XmlResolver = null };
            check.LoadXml("<Content>" + inner + "</Content>");
            _data = TriggerData.Parse(inner);
            RefreshAll();
            MessageBox.Show(this, "Trigger XML parsed and applied.", "Trigger Logic", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"The trigger XML is invalid.\\n\\n{ex.Message}", "Trigger Logic", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            CommitVariables();
            var xml = _data.ToXml();
            var doc = new XmlDocument { XmlResolver = null };
            doc.LoadXml(xml);
            TriggerXml = doc.DocumentElement?.InnerXml ?? "";
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save trigger XML.\\n\\n{ex.Message}", "Trigger Logic", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void SetParam(System.Collections.Generic.List<TParam> parameters, string attr, string text)
    {
        var parameter = parameters.FirstOrDefault(p => p.attr == attr);
        if (parameter is null) { parameter = new TParam(attr, TValue.Parse(text)); parameters.Add(parameter); }
        else SetValue(parameter.v, text);
    }
    private static void SetValue(TValue target, string text)
    {
        var value = TValue.Parse(text);
        target.mode = value.mode;
        target.text = value.text;
        target.varName = value.varName;
        target.modelIsVar = value.modelIsVar;
        target.model = value.model;
        target.prop = value.prop;
        target.nodeIsVar = value.nodeIsVar;
        target.node = value.node;
        target.coord = value.coord;
    }
}