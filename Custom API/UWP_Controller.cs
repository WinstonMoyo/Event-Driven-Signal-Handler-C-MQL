using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace Custom_API.Controllers
{
    // This controller handles API requests for the UWP application.
    // The routes are prefixed with "myAPI" and handle POST requests to receive messages 
    // and GET requests to retrieve stored JSON messages.

    [ApiController]
    [Route("myAPI")]
    public class UwpController : ControllerBase
    {
        /// <summary>
        /// This action method handles POST requests sent to "myAPI/sendMessage".
        /// It reads the request body (expected to be JSON data) and stores it in a static storage.
        /// </summary>
        /// <returns>Returns a success message if the request is valid, otherwise returns an error.</returns>
        [HttpPost("sendMessage")]
        public async Task<IActionResult> PostFromUwp()
        {
            // Ensure the incoming request is a POST request
            if (Request.Method.ToUpper() != "POST")
            {
                return BadRequest("Only POST requests are allowed.");
            }

            // Check if the request body is readable (i.e., not empty)
            if (!Request.Body.CanRead)
            {
                return BadRequest("Request body is empty.");
            }

            // Create a MemoryStream to hold the incoming request body (JSON message)
            MemoryStream? jsonMessage = new MemoryStream();

            // Asynchronously copy the request body into the MemoryStream
            await Request.Body.CopyToAsync(jsonMessage);

            // Reset the stream's position to the beginning (necessary for reading)
            jsonMessage.Seek(0, SeekOrigin.Begin);

            // Read the entire stream and store the content as a string in the Storage class
            Storage.message = await new StreamReader(jsonMessage).ReadToEndAsync();

            // Log the received message for debugging purposes
            Console.WriteLine(Storage.message);

            // Return a success response indicating that the message was received
            return Ok("Message Received");
        }

        /// <summary>
        /// This action method handles GET requests sent to "myAPI/storedjson".
        /// It retrieves the last stored JSON message from the static storage.
        /// </summary>
        /// <returns>Returns the stored message or a 404 if no message has been stored.</returns>
        [HttpGet("storedjson")]
        public IActionResult GetStoredJson()
        {
            // If there's no stored message, return a 404 Not Found response
            if (string.IsNullOrEmpty(Storage.message))
            {
                return NotFound("No JSON request stored yet.");
            }

            // Temporarily store the message to return, then clear the stored message
            string temp = Storage.message;
            Storage.message = string.Empty;

            // Return the stored JSON message
            return Ok(temp);
        }
    }
}
