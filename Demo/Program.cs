using System;
using System.IO;
using CommandLine;
using Google.Protobuf;
using log4net;
using Newtonsoft.Json;
using POGOLib.Net;
using POGOLib.Net.Authentication;
using POGOLib.Net.Authentication.Data;
using POGOLib.Pokemon.Data;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;

namespace Demo
{
    public class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Program));

        static Session session;
        /// <summary>
        ///     This is just a demo application to test out the library / show a bit how it works.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Log.Info("Booting up.");
            Log.Info("Type 'q', 'quit' or 'exit' to exit.");
            Console.Title = "POGO Demo";

            var arguments = new Arguments
            {
                LoginProvider = "PTC",
                Password = "cafe7web7",
                Username = "chebusin",//Spoll1961
                Debug = true
            };

    //        var session = Login.GetSession(arguments.Username, arguments.Password, LoginProvider.PokemonTrainerClub, -58.4760243, -34.5430915);
    //        session.Player.Inventory.Update += (sender, eventArgs) =>
    //        {
    //// Access updated inventory: session.Player.Inventory
    //Console.WriteLine("Inventory was updated.");
    //        };
    //        session.Map.Update += (sender, eventArgs) =>
    //        {
    //// Access updated map: session.Map
    //Console.WriteLine("Map was updated.");
    //        };

            var latitude = -34.543708; // Somewhere cerca de la ofi
                var longitude = -58.474372;
                session = GetSession(arguments.Username, arguments.Password, arguments.LoginProvider, latitude,longitude, true);

                SaveAccessToken(session.AccessToken);

                session.AccessTokenUpdated += SessionOnAccessTokenUpdated;
                session.Player.Inventory.Update += InventoryOnUpdate;
                session.Map.Update += MapOnUpdate;


                // Send initial requests and start HeartbeatDispatcher
                session.Startup();

            //session.RpcClient.RefreshMapObjects();


            //var fortDetailsBytes = session.RpcClient.SendRemoteProcedureCall(new Request
            //{
            //    RequestType = RequestType.FortDetails,
            //    RequestMessage = new FortDetailsMessage
            //    {
            //        FortId = "e4a5b5a63cf34100bd620c598597f21c.12",
            //        Latitude = 51.507335,
            //        Longitude = -0.127689
            //    }.ToByteString()
            //});
            //var fortDetailsResponse = FortDetailsResponse.Parser.ParseFrom(fortDetailsBytes);

            //Console.WriteLine(JsonConvert.SerializeObject(fortDetailsResponse, Formatting.Indented));


            HandleCommands();
        }

        private static void SessionOnAccessTokenUpdated(object sender, EventArgs eventArgs)
        {
            var session = (Session) sender;

            SaveAccessToken(session.AccessToken);

            Log.Info("Saved access token to file.");
        }

        private static void InventoryOnUpdate(object sender, EventArgs eventArgs)
        {
            Log.Info("Inventory was updated.");
        }

        private static void MapOnUpdate(object sender, EventArgs eventArgs)
        {
            var cachablePokemons = session.Map.GetCatchablePokemonsSortedByDistance();
            Log.Info($"catchable pokemons:{cachablePokemons.Count}");
            var nearbyPokemons = session.Map.GetNearbyPokemons();
            Log.Info($"nearby pokemons:{nearbyPokemons.Count}");
            var wildPokemon = session.Map.GetWildPokemonsSortedByDistance();
            Log.Info($"wildPokemon pokemons:{wildPokemon.Count}");
            //Dame todos los pokemones q puedo atrapar
            foreach (var item in cachablePokemons)
            {                   
                Console.WriteLine(item.PokemonId);
            }

            //Dame todos los pokemones q estan cerca
            foreach (var item in nearbyPokemons)
            {              
                Console.WriteLine(item.PokemonId);
            }

            //Dame todos los pokemones q salvajes
            foreach (var item in wildPokemon)
            {

                Console.WriteLine(item.EncounterId);
            }
            Log.Info("Map was updated.");
            session.Player.WalkAround();

        }

        private static void SaveAccessToken(AccessToken accessToken)
        {
            var fileName = Path.Combine(Environment.CurrentDirectory, "cache", $"{accessToken.Uid}.json");

            File.WriteAllText(fileName, JsonConvert.SerializeObject(accessToken, Formatting.Indented));
        }

        private static void HandleCommands()
        {
            var keepRunning = true;

            while (keepRunning)
            {
                var command = Console.ReadLine();

                switch (command)
                {
                    case "l":
                        
                    case "q":
                    case "quit":
                    case "exit":
                        keepRunning = false;
                        break;
                }
            }
        }

        /// <summary>
        ///     Login to PokémonGo and return an authenticated <see cref="Session" />.
        /// </summary>
        /// <param name="username">The username of your PTC / Google account.</param>
        /// <param name="password">The password of your PTC / Google account.</param>
        /// <param name="loginProviderStr">Must be 'PTC' or 'Google'.</param>
        /// <param name="initLat">The initial latitude.</param>
        /// <param name="initLong">The initial longitude.</param>
        /// <param name="mayCache">Can we cache the <see cref="AccessToken" /> to a local file?</param>
        private static Session GetSession(string username, string password, string loginProviderStr, double initLat,
            double initLong, bool mayCache = false)
        {
            var loginProvider = ResolveLoginProvider(loginProviderStr);
            var cacheDir = Path.Combine(Environment.CurrentDirectory, "cache");
            var fileName = Path.Combine(cacheDir, $"{username}-{loginProvider}.json");

            if (mayCache)
            {
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                if (File.Exists(fileName))
                {
                    var accessToken = JsonConvert.DeserializeObject<AccessToken>(File.ReadAllText(fileName));

                    if (!accessToken.IsExpired)
                        return Login.GetSession(accessToken, password, initLat, initLong);
                }
            }

            var session = Login.GetSession(username, password, loginProvider, initLat, initLong);

            if (mayCache)
                SaveAccessToken(session.AccessToken);

            return session;
        }

        private static LoginProvider ResolveLoginProvider(string loginProvider)
        {
            switch (loginProvider)
            {
                case "PTC":
                    return LoginProvider.PokemonTrainerClub;
                case "Google":
                    return LoginProvider.GoogleAuth;
                default:
                    throw new Exception($"The login method '{loginProvider}' is not supported.");
            }
        }
    }
}