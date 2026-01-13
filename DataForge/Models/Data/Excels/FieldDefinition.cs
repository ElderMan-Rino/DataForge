namespace Elder.DataForge.Models.Data.Excels
{
    public readonly struct FieldDefinition
    {
        public readonly int FieldOrder;
        public readonly string VariableName;
        public readonly string VariableType;

        public FieldDefinition(int fieldOrder, string variableName, string variableType)
        {
            FieldOrder = fieldOrder;
            VariableName = variableName;
            VariableType = variableType;
        }
    }
}
