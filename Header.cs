namespace Sockets
{
    public class Header
    {
        public Header(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public readonly string Name;
        public readonly string Value;
    }
}