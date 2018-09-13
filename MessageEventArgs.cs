namespace ModbusMasterIO
{
    internal delegate void StateHandler(object sender, MessageEventArgs e);

    internal class MessageEventArgs
    {
        public string Message { get; set; }
        public int CurrentLevel { get; set; }

        public MessageEventArgs(string message, int level)
        {
            Message = message;
            CurrentLevel = level;
        }
    }
}
