using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Background;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Notifications;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Notifications.Management;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using System.Net;
using System.Runtime.InteropServices;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Telegram_Signal_Copier___Demo
{
    /// <summary>
    /// The MainPage class is the main entry point for the application's user interface.
    /// This is where the core logic for the UWP application, responsible for listening
    /// to notifications and sending signals to the API, is implemented.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Method to check the user's notifications and process the most recent notification.
        /// This listens for notifications from specific apps (like WhatsApp, Telegram, Discord).
        /// </summary>
        /// <returns>Returns true if a valid trade signal is found, false otherwise.</returns>
        public async Task<bool> checkNotif()
        {
            // Check if the Notification Listener API is available
            if (ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
            {
                // Get the current notification listener
                UserNotificationListener listener = UserNotificationListener.Current;

                // Request access to notifications
                UserNotificationListenerAccessStatus accessStatus = await listener.RequestAccessAsync();

                try
                {
                    // Check the access status and handle accordingly
                    switch (accessStatus)
                    {
                        case UserNotificationListenerAccessStatus.Allowed:
                            // Access to notifications is granted
                            break;
                        case UserNotificationListenerAccessStatus.Denied:
                            Console.WriteLine("Notification access denied.");
                            break;
                        case UserNotificationListenerAccessStatus.Unspecified:
                            // Access status is unspecified
                            break;
                    }

                    // Retrieve all toast notifications
                    IReadOnlyList<UserNotification> notifs = await listener.GetNotificationsAsync(NotificationKinds.Toast);

                    // Get the most recent notification
                    UserNotification notif = notifs[notifs.Count - 1];
                    string appName = notif.AppInfo.DisplayInfo.DisplayName;

                    // Extract text from the notification
                    var text = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                    IReadOnlyList<AdaptiveNotificationText> textEl = text.GetTextElements();
                    string groupName = textEl[0].Text; // Group or channel name
                    string bodyText = string.Join("\n", textEl.Skip(1).Select(t => t.Text)); // Notification body

                    // Check if the notification comes from one of the specified groups
                    foreach (string group in groups)
                    {
                        if (appName.Contains(instancePathway.app) && groupName.Contains(group))
                        {
                            try
                            {
                                // Check if the notification contains trade signals
                                if (bodyText.Contains("Long") || bodyText.Contains("Short") || bodyText.Contains("BUY") || bodyText.Contains("SELL"))
                                {
                                    // Update the UI to show the trade signal
                                    messageWindow.Text = bodyText;

                                    // Send the trade signal to the API
                                    ApiClient apiClient = new ApiClient("http://localhost:" + port + "/myAPI/sendMessage");
                                    var responseCode = await apiClient.SendTextToApiAsync(ParseTradeSignal(bodyText));

                                    return true; // Trade signal found
                                }
                                else
                                {
                                    // No valid signal found
                                    messageWindow.Text = "No Signal Found...";
                                }
                                return false;
                            }
                            catch (Exception)
                            {
                                // Handle any exceptions during processing
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message); // Log any errors
                }
            }
            else
            {
                return false; // Notification listener API is not available
            }
            return false;
        }

        /// <summary>
        /// Parses a trade signal from the notification text.
        /// Extracts key information such as operation (BUY/SELL), symbol, entry price, stop loss, and take profit.
        /// </summary>
        /// <param name="signal">The notification text containing the trade signal.</param>
        /// <returns>A JObject containing the parsed trade signal details.</returns>
        public static JObject ParseTradeSignal(string signal)
        {
            // Initialize variables to hold parsed data
            string operation = "";
            string symbol = "";
            string entryPrice = "";
            string stopLoss = "";
            string takeProfit = "";

            bool sl = false;
            bool tp = false;
            bool entry = false;
            bool op = false;

            // Split the notification text into individual lines
            string[] all_strings = signal.Split('\n');

            // Loop through each line and parse relevant information
            foreach (string d in all_strings)
            {
                string[] deep = d.Split(' ');

                if (deep.Length == 1)
                {
                    deep = d.Split(':');
                }

                foreach (string s in deep)
                {
                    // Check for operation type (BUY/SELL)
                    if (s.Contains("buy") || s.Contains("sell") || s.Contains("long") || s.Contains("short"))
                    {
                        operation = s;
                        op = true;
                        continue;
                    }

                    // Check for stop loss (SL) information
                    if (s.Contains("SL") || s.Contains("Stop Loss"))
                    {
                        sl = true;
                        continue;
                    }

                    // Check for take profit (TP) information
                    if (s.Contains("TP") || s.Contains("Take Profit"))
                    {
                        tp = true;
                        continue;
                    }

                    // Check for entry price information
                    if (s.Contains("Entry") || s.Contains("Enter"))
                    {
                        if (entryPrice == "")
                        {
                            entry = true;
                        }
                        continue;
                    }

                    int count = 0;
                    int letters = 0;
                    string val = "";

                    // Loop through characters to extract numbers (prices)
                    foreach (char letter in s)
                    {
                        if (char.IsDigit(letter) || letter == '.')
                        {
                            count++;
                            val = val + letter;
                        }

                        if (char.IsLetter(letter))
                        {
                            letters++;
                        }
                    }

                    // Identify symbol and reset operation flag if necessary
                    if ((letters >= 6 || (letters > 0 && count > 0)) && !s.Contains(":") && op == true ? true : s == all_strings[0])
                    {
                        symbol = s;
                        op = op == true ? false : op;
                        continue;
                    }

                    // Extract numeric values for stop loss and take profit
                    if (s.Length == count)
                    {
                        if (sl)
                        {
                            stopLoss = val;
                            sl = false;
                            break;
                        }

                        if (tp)
                        {
                            takeProfit = takeProfit + ", " + val;
                            tp = false;
                        }
                    }

                    // Extract entry price if it hasn't been captured yet
                    if (entry && !sl && !tp)
                    {
                        entryPrice = entryPrice + ", " + val;

                        if (s == deep[deep.Length - 1])
                        {
                            entry = false;
                            break;
                        }
                    }

                    continue;
                }
            }

            // Construct a JSON object with the parsed trade signal information
            JObject tradeSignalJson = new JObject(
                new JProperty("operation", operation),
                new JProperty("symbol", symbol),
                new JProperty("entry_price", entryPrice),
                new JProperty("stop_loss", stopLoss),
                new JProperty("take_profit", takeProfit)
            );

            return tradeSignalJson;
        }

        // Struct to hold the message pathway information (app and group/channel name)
        struct messagePathway
        {
            public string app { get; set; }
            public string pathway { get; set; }
        };

        private static messagePathway instancePathway;

        private bool isSearching = false; // Indicates if the listener is actively searching
        DispatcherTimer timer = new DispatcherTimer(); // Timer for checking notifications periodically
        private bool listenerStarted;
        private bool portSet;
        private string port; // Port number for API communication
        private string apiExePath;
        private bool api_set;
        private string[] groups; // Array of group names to listen for

        // MainPage constructor that initializes the UI components and settings
        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(350, 350));
            ApplicationView.PreferredLaunchViewSize = new Size(350, 350);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            listenerBtn.Content = "Search"; // Set initial search button text
            listenerBtn.Background = new SolidColorBrush(Windows.UI.Colors.Blue); // Set button color
            listenerBtn.Visibility = Visibility.Collapsed; // Hide the search button initially
            messageWindow.Text = "No Signal"; // Initial message in the window

            timer.Tick += Timer_Tick; // Timer event handler
            timer.Interval = TimeSpan.FromSeconds(1); // Timer interval (every second)

            api_set = false; // API setup status
            listenerStarted = false;

            appName.Text = "Select App Name"; // Placeholder for app name

            port_Number.Text = "Port Number: "; // Placeholder for port number

            // Observable collection of app items for the ComboBox (WhatsApp, Telegram, Discord)
            ObservableCollection<AppItem> appItems = new ObservableCollection<AppItem>
            {
                new AppItem { Name = "WhatsApp" },
                new AppItem { Name = "Telegram" },
                new AppItem { Name = "Discord" },
            };

            // Assign the collection to the ComboBox's ItemsSource
            appName.ItemsSource = appItems;

            messageStatus.Text = "Not Set"; // Initial message status
            messageStatus.Foreground = new SolidColorBrush(Windows.UI.Colors.Red); // Set text color to red
            groupName.Text = "Type Group/Channel Name/s separated by ';'"; // Placeholder for group/channel name

            setMessageBtn.Content = "Set Message Pathway"; // Set button text
            resetBtn.Content = "Reset"; // Set reset button text
        }

        // Event handler for setting the message pathway (app, group, and port)
        private void setMessageBtn_Click(object sender, RoutedEventArgs e)
        {
            object selectedItem = appName.SelectedItem;

            if (selectedItem != null && groupName.Text != "Type Group/Channel Name" && port_Number.Text != "" && port_Number.Text != "Port Number: ")
            {
                AppItem selectedAppItem = (AppItem)selectedItem;
                string selectedText = selectedAppItem.Name;
                instancePathway.app = selectedText;
                instancePathway.pathway = groupName.Text;
                messageStatus.Text = "Set"; // Indicate that the pathway is set
                messageStatus.Foreground = new SolidColorBrush(Windows.UI.Colors.Blue); // Set color to blue
                listenerBtn.Visibility = Visibility.Visible; // Show the listener button
                groups = groupName.Text.Split(';'); // Split group names by ';'
                port_Number.Visibility = Visibility.Collapsed;
                port = port_Number.Text;
            }
            else
            {
                // Show an error message if required fields are not completed
                ShowMessageBox("Please Complete All Fields");
            }
        }

        // Helper method to show a message box with an alert
        private async void ShowMessageBox(string message)
        {
            var messageDialog = new MessageDialog(message, "Alert");
            messageDialog.Commands.Add(new UICommand("OK", null));
            await messageDialog.ShowAsync();
        }

        // Event handler to reset the message pathway and UI fields
        private void resetBtn_Click(object sender, RoutedEventArgs e)
        {
            instancePathway.app = "";
            instancePathway.pathway = "";
            appName.SelectedItem = null;
            groupName.Text = "Type Group/Channel Name";
            messageStatus.Text = "Not Set";
            messageStatus.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
            listenerBtn.Visibility = Visibility.Collapsed;
        }

        // Event handler to start/stop searching for notifications
        private void listenerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isSearching)
            {
                isSearching = true;
                listenerBtn.Content = "Stop Searching";
                listenerBtn.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                timer.Start(); // Start the timer to check notifications
            }
            else if (isSearching)
            {
                isSearching = false;
                listenerBtn.Content = "Search";
                listenerBtn.Background = new SolidColorBrush(Windows.UI.Colors.Blue);
                timer.Stop(); // Stop the timer
            }
        }

        // Timer tick event handler that checks notifications every interval
        private async void Timer_Tick(object sender, object e)
        {
            // Check if a notification contains a valid trade signal
            if (await checkNotif())
            {
                // ShowMessageBox("Success");
            }
            else
            {
                // ShowMessageBox("Fail");
            }
        }

        // Class to represent an app item for selection (e.g., WhatsApp, Telegram, Discord)
        public class AppItem
        {
            public string Name { get; set; }
        }
    }
}
