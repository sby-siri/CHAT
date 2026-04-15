namespace Chat.Models
{
    /// <summary>
    /// Message type to be sed for communication
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; } = ""; //"system", "user", "assistant"

        public string Content { get; set; } = "";
    }
}
