namespace Custom_API
{
    // A static class to hold data that needs to be shared across different parts of the application.
    // In this case, it stores a message received from the UWP application.
    public static class Storage
    {
        // A public static variable to store the message. The "?" indicates it can be null.
        public static string? message { get; set; }

        // Static constructor to initialize the message property when the class is first accessed.
        // This sets the message to an empty string by default, avoiding null references.
        static Storage()
        {
            message = string.Empty;
        }
    }
}
