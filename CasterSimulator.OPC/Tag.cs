using Opc.Ua;

namespace CasterSimulator.OPC;

public class Tag
{
    public string TagName { get; set; }
    public string OpcItemName { get; set; }
    public object Value { get; set; }
    public BuiltInType DataType { get; set; }
}
