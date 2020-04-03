namespace RechargeTools.Models
{
    public class TableFilterViewModel
    {
        public string type { get; set; }
        public int draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public TableFilterSearchViewModel search { get; set; }
        public TableFilterOrderViewModel[] order { get; set; }
    }

    public class TableFilterSearchViewModel
    {
        public string value { get; set; }
        public bool regex { get; set; }
    }

    public class TableFilterOrderViewModel
    {
        public int column { get; set; }
        public string dir { get; set; }
    }

    public class EditorTableViewModel
    {
        public string action { get; set; }
        public object[] data { get; set; }
    }
}