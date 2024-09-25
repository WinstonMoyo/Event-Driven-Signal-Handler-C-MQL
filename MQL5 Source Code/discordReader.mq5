//+------------------------------------------------------------------+
//|                                                discordReader.mq5 |
//|                                                Winston Moyo      |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Winston Moyo"
#property link      ""
#property version   "1.00"
#include <Jason.mqh> // Include the JSON parser library for processing JSON data
#include <Trade/Trade.mqh> // Include the trading library for trade operations

// Enumeration to define different risk types
enum RiskType
  {
   PERCENT = 0, // Risk as a percentage of balance
   AMOUNT = 1,  // Risk a specific dollar amount
   FIXED = 2    // Fixed lot size
  };

// Input settings for the API and trade settings
input string aa = "------------------------------------"; //------------API Settings------------
input int portNum = 0000; //Enter Port Number Here for API
input string bb = "------------------------------------"; //------------Trade Settings------------
input RiskType typeRisk = PERCENT; // Select risk type (Percentage, Fixed Lot, or Amount)
input double perc_risk = 0.02; // Percentage of risk per trade (if using percentage risk)
input double lot_size = 0.10; // Fixed lot size (if using fixed lot size)
input double amountRisk = 200; // Dollar amount to risk (if using amount risk)
input bool breakeven = true; // Enable/Disable breakeven feature
input double pipsToBreak = 10; // Pips to move to breakeven
input bool partialClose = false; // Enable/Disable partial close for multiple take-profits

// URL of the local API to fetch trade signals
string url = "http://127.0.0.1:" + (string)portNum + "/myAPI/storedjson";
double Profits[]; // Array to hold take-profit levels

// Struct to store trade details
struct TRADE
  {
   string symbol;       // Trading symbol
   double entry;        // Entry price
   double sl;           // Stop loss
   double tp[];         // Array of take-profit levels
   string type;         // Trade type (Buy or Sell)
   int ticket;          // Trade ticket number
   double lot_size;     // Lot size for the trade
   int multiplier;      // Multiplier to convert pips into points
   double split_vol;    // Volume for partial close
  };

TRADE myTrades[]; // Array to store ongoing trades

CTrade myTrader; // Object to handle trade execution
int second = 0;  // Counter to control the timer

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   // Set a timer to execute every second
   EventSetTimer(1);
   return(INIT_SUCCEEDED); // Return success upon initialization
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   // Destroy the timer when the expert is removed
   EventKillTimer();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   // Empty as this strategy does not depend on market ticks
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
   // This function is called every second
   manageTrades(); // Manage any open trades

   // Fetch new signals every 10 seconds
   if(second == 10)
     {
      GetRequest(url); // Send request to the API to get trade signal
      second = 0; // Reset the counter after fetching the signal
     }

   second++; // Increment the counter
  }
//+------------------------------------------------------------------+
//| Trade function                                                   |
//+------------------------------------------------------------------+
void OnTrade()
  {
   // This function can be used to handle trade events (currently empty)
  }
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& trans,
                        const MqlTradeRequest& request,
                        const MqlTradeResult& result)
  {
   // Handle trade transactions (currently not used)
  }
//+------------------------------------------------------------------+
//| Tester function                                                  |
//+------------------------------------------------------------------+
double OnTester()
  {
   // For use in strategy tester (currently returns 0)
   return(0.0);
  }
//+------------------------------------------------------------------+
//| Function to send HTTP GET request and handle the response        |
//+------------------------------------------------------------------+
void GetRequest(string my_url)
  {
   char result[];         // Array to store the result of the request
   char data[];           // Placeholder for request data (not used)
   string resultHeaders;  // Store response headers
   CJAVal myJS;           // JSON object to parse the result

   // Send HTTP GET request to the specified URL
   int requestResult = WebRequest("GET", my_url, "", 0, data, result, resultHeaders);

   // Check if the request was successful and not a 404 error
   if(requestResult != -1 && requestResult != 404)
     {
      // Deserialize JSON response into the myJS object
      myJS.Deserialize(result);

      // Split the take-profit levels from the JSON response
      string takeProfits[];
      int size = StringSplit(myJS["take_profit"].ToStr(), ',', takeProfits);

      // Extract trading symbol and stop loss from the JSON response
      string chart = myJS["symbol"].ToStr();
      string sl = myJS["stop_loss"].ToStr();

      // Call enterTrade() function to execute the trade based on the received signal
      if(enterTrade(myJS["operation"].ToStr(), chart, sl, myJS["take_profit"].ToStr()))
        {
         Alert("Trade Opened Successfully"); // Alert if trade is opened successfully
        }

      // Log trade details for debugging purposes
      Print("OP: " + myJS["operation"].ToStr());
      Print("Symbol: " + myJS["symbol"].ToStr());
      Print("Entry: " + myJS["entry_price"].ToStr());
      Print("SL: " + myJS["stop_loss"].ToStr());
      Print("TP: " + myJS["take_profit"].ToStr());
     }
   else
     {
      // Log error if the HTTP request fails
     // Print("Error: Failed to send HTTP request");
     }

  }

//+------------------------------------------------------------------+
//| Function to process the trade signal and open a trade            |
//+------------------------------------------------------------------+
bool enterTrade(string operation, string &symbol, string &sl, string tp)
  {
   // Process take-profit values from the received signal
   string takes[];
   StringSplit(tp, ',', takes);
   for(int i = 0; i < (int)takes.Size(); i++)
     {
      if(takes[i] != "")
        {
         ArrayResize(Profits, Profits.Size() + 1);
         Profits[Profits.Size()-1] = StringToDouble(takes[i]);
        }
     }

   // Process stop-loss values from the received signal
   string stops[];
   StringSplit(sl, ',', stops);
   for(int i = 0; i < (int)stops.Size(); i++)
     {
      if(stops[i] != "")
        {
         sl = stops[i];
        }
     }

   // Convert trading symbol if it includes a separator
   string symbs[];
   if(StringSplit(symbol, '/', symbs) > 1)
     {
      symbol = symbs[0] + symbs[1];
     }

   // Check if the trade for the given symbol already exists
   if(myTrades.Size() > 0)
     {
      for(int i = 0; i < (int)myTrades.Size(); i++)
        {
         if(symbol == myTrades[i].symbol)
           {
            return false; // Do not open duplicate trades for the same symbol
           }
        }
     }
 
   // Process buy trades
   if(operation == "BUY" || operation == "Buy" || operation == "buy" || operation == "Long" || operation == "LONG" || operation == "long")
     {
      // Calculate lot size based on risk settings
      double lotSize = getLots(perc_risk,StringToDouble(sl), SymbolInfoDouble(symbol, SYMBOL_ASK), getPipValue(symbol), SymbolInfoDouble(symbol, SYMBOL_POINT));
      lotSize = lotSize < SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN) ? SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN) : (lotSize > SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX) ? SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX) : lotSize);
      lotSize = floor(lotSize); // Floor the lot size to avoid decimals

      // Open the buy trade and check for success
      if(!myTrader.Buy(lotSize, symbol, SymbolInfoDouble(symbol, SYMBOL_ASK), StringToDouble(sl), Profits[Profits.Size()-1]))
        {
         Alert("Fail to open buy trade for: " + symbol);
         return false;
        }
      else
        {
         // Add trade to the myTrades array for tracking
         ArrayResize(myTrades, myTrades.Size() + 1);
         myTrades[myTrades.Size() - 1].entry = SymbolInfoDouble(symbol, SYMBOL_ASK);
         myTrades[myTrades.Size() - 1].symbol = symbol;
         myTrades[myTrades.Size() - 1].sl = StringToDouble(sl);
         ArrayCopy(myTrades[myTrades.Size() - 1].tp, Profits);
         myTrades[myTrades.Size() - 1].ticket = myTrader.ResultOrder();
         return true;
        }
     }
   return false; // Return false if trade cannot be opened
  }
  
//+------------------------------------------------------------------+
//| Function to calculate the lot size based on risk settings        |
//+------------------------------------------------------------------+
double getLots(double perc_risk, double sl, double entry_price, double pipValue, double pipPoints)
  {
   // Calculate the risk in terms of dollars
   double riskAmount = AccountInfoDouble(ACCOUNT_BALANCE) * perc_risk;

   // Calculate the number of pips to the stop loss
   double pipsToSL = MathAbs(entry_price - sl) / pipPoints;

   // Calculate the lot size based on the risk and stop loss
   return riskAmount / (pipsToSL * pipValue);
  }
  
//+------------------------------------------------------------------+
//| Function to manage open trades                                   |
//+------------------------------------------------------------------+
void manageTrades()
  {
   // Iterate through all open trades and apply break-even or close trades if conditions are met
   for(int i = 0; i < ArraySize(myTrades); i++)
     {
      double profit = myTrades[i].entry + (pipsToBreak * getPipValue(myTrades[i].symbol));
      if(breakeven && SymbolInfoDouble(myTrades[i].symbol, SYMBOL_BID) >= profit)
        {
         myTrader.Modify(myTrades[i].ticket, SymbolInfoDouble(myTrades[i].symbol, SYMBOL_BID), myTrades[i].tp[myTrades[i].tp.Size() - 1]);
        }
     }
  }
  
//+------------------------------------------------------------------+
//| Function to get pip value based on the trading symbol            |
//+------------------------------------------------------------------+
double getPipValue(string symbol)
  {
   if(StringFind(symbol, "JPY") >= 0)
     return 0.01; // JPY pairs typically have pips measured in 0.01
   return 0.0001; // Other pairs typically have pips measured in 0.0001
  }
