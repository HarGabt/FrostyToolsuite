namespace BlueprintEditorPlugin.Models.Entities.Networking
{
    /// <summary>
    /// The "Realm" or machines this entity operates on.
    /// Any - Blueprint Editor automatically determines any of these based on the first associated networkable found with a valid realm
    /// Invalid - This networkable is broken, and will rely on whatever fallback the game has. E.g, if invalid, UI will function since it's always ran client side.
    /// ClientAndServer - 2 copies of this connection will run side by side, one on the server's machine, one on the clients. This does NOT mean they are synced across both client and server.
    /// Client - This is ran exclusively on the clients machine, and is not synced across all machines.
    /// Server - This is ran exclusively on the servers machine, and this exact networkable is synced identically for all clients.
    /// NetworkedClient - This is ran on the clients machine, and is synced across other clients through the server.
    /// NetworkedClientAndServer - Same as ClientAndServer, except with NetworkedClient.
    /// </summary>
    public enum Realm
    {
        Any = -1,
        Invalid = 0,
        ClientAndServer = 1,
        Client = 2, 
        Server = 3,
        NetworkedClient = 4,
        NetworkedClientAndServer = 5,
        //Any = -1
    }
    
    public enum PropertyType
    {
        Default = 0,
        Interface = 1,
        Exposed = 2, //TODO: Figure out when Exposed is used
        Invalid = 3
    }
}