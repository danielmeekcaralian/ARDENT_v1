using System.Collections.Generic;

// Pure graph rules: independent of AR positions, rendering and lesson rewards.
public static class NetworkValidator
{
    public readonly struct Node
    {
        public readonly int id;
        public readonly NetworkNodeType type;
        public readonly string label;
        public Node(int id, NetworkNodeType type, string label)
        { this.id = id; this.type = type; this.label = label; }
    }

    public readonly struct Edge
    {
        public readonly int a, b;
        public Edge(int a, int b) { this.a = a; this.b = b; }
    }

    public readonly struct Result
    {
        public readonly bool passed;
        public readonly string message;
        public Result(bool passed, string message) { this.passed = passed; this.message = message; }
    }

    public static Result CheckStar(IReadOnlyList<Node> nodes, IReadOnlyList<Edge> edges, int requiredPCs = 4)
    {
        if (nodes == null || edges == null || requiredPCs < 1)
            return Fail("The Star activity configuration is incomplete.");
        var byID = new Dictionary<int, Node>();
        int pcs = 0, switches = 0, others = 0, switchID = 0;
        foreach (var node in nodes)
        {
            if (byID.ContainsKey(node.id)) return Fail("Duplicate device identity. Restart the activity.");
            byID.Add(node.id, node);
            if (node.type == NetworkNodeType.PC) pcs++;
            else if (node.type == NetworkNodeType.Switch) { switches++; switchID = node.id; }
            else others++;
        }
        if (pcs != requiredPCs || switches != 1 || others != 0)
            return Fail($"Use exactly {requiredPCs} PCs and 1 switch. Currently: {pcs} PCs, {switches} switches, {others} other devices. Add missing devices or delete extras.");

        var connected = new HashSet<int>();
        var pairs = new HashSet<(int, int)>();
        foreach (var edge in edges)
        {
            if (!byID.TryGetValue(edge.a, out var a) || !byID.TryGetValue(edge.b, out var b))
                return Fail("A cable has an endpoint outside this activity. Remove it and reconnect.");
            if (edge.a == edge.b) return Fail("A device cannot connect to itself.");
            var pair = edge.a < edge.b ? (edge.a, edge.b) : (edge.b, edge.a);
            if (!pairs.Add(pair)) return Fail($"Duplicate cable between {a.label} and {b.label}. Keep only one.");
            if (edge.a != switchID && edge.b != switchID)
                return Fail($"Remove the direct connection between {a.label} and {b.label}. Each PC must connect only to the switch.");
            connected.Add(edge.a == switchID ? edge.b : edge.a);
        }
        var missing = new List<string>();
        foreach (var node in nodes)
            if (node.type == NetworkNodeType.PC && !connected.Contains(node.id)) missing.Add(node.label);
        if (missing.Count > 0) return Fail("Connect these PCs to the switch: " + string.Join(", ", missing) + ".");
        return new Result(true, $"STAR TOPOLOGY COMPLETE\n{requiredPCs} PCs connected to one switch, with no extra connections.");
    }

    public static Result CheckRing(IReadOnlyList<Node> nodes, IReadOnlyList<Edge> edges, int requiredPCs = 4)
    {
        if (nodes == null || edges == null || requiredPCs < 3)
            return RingFail("The Ring activity configuration is incomplete.");
        var byID = new Dictionary<int, Node>();
        var neighbors = new Dictionary<int, HashSet<int>>();
        int pcs = 0;
        foreach (var node in nodes)
        {
            if (byID.ContainsKey(node.id)) return RingFail("Duplicate device identity. Restart the activity.");
            byID.Add(node.id, node);
            neighbors.Add(node.id, new HashSet<int>());
            if (node.type == NetworkNodeType.PC) pcs++;
        }
        if (pcs != requiredPCs || nodes.Count != requiredPCs)
            return RingFail($"Use exactly {requiredPCs} PCs and no switch or backbone. Currently: {pcs} PCs and {nodes.Count - pcs} other devices. Add missing PCs or delete extras.");
        foreach (var edge in edges)
        {
            if (!byID.ContainsKey(edge.a) || !byID.ContainsKey(edge.b))
                return RingFail("A cable has an endpoint outside this activity. Remove it and reconnect.");
            if (edge.a == edge.b) return RingFail("A device cannot connect to itself.");
            if (!neighbors[edge.a].Add(edge.b))
                return RingFail($"Duplicate cable between {byID[edge.a].label} and {byID[edge.b].label}. Keep only one.");
            neighbors[edge.b].Add(edge.a);
        }
        foreach (var node in nodes)
            if (neighbors[node.id].Count != 2)
                return RingFail($"{node.label} has {neighbors[node.id].Count} connections. Each PC needs exactly 2 connections to different PCs.");
        var reached = new HashSet<int>();
        var pending = new Stack<int>();
        pending.Push(nodes[0].id);
        while (pending.Count > 0)
        {
            int id = pending.Pop();
            if (!reached.Add(id)) continue;
            foreach (int neighbor in neighbors[id]) pending.Push(neighbor);
        }
        if (reached.Count != nodes.Count)
            return RingFail("The PCs form separate loops. Connect all PCs into one closed loop.");
        return new Result(true, $"RING TOPOLOGY COMPLETE\n{requiredPCs} PCs form one closed loop, with exactly 2 connections each.");
    }

    public static Result CheckBus(IReadOnlyList<Node> nodes, IReadOnlyList<Edge> edges, int requiredPCs = 4)
    {
        if (nodes == null || edges == null || requiredPCs < 1)
            return BusFail("The Bus activity configuration is incomplete.");
        var byID = new Dictionary<int, Node>();
        int pcs = 0, backbones = 0, others = 0, backboneID = 0;
        foreach (var node in nodes)
        {
            if (byID.ContainsKey(node.id)) return BusFail("Duplicate device identity. Restart the activity.");
            byID.Add(node.id, node);
            if (node.type == NetworkNodeType.PC) pcs++;
            else if (node.type == NetworkNodeType.Backbone) { backbones++; backboneID = node.id; }
            else others++;
        }
        if (pcs != requiredPCs || backbones != 1 || others != 0)
            return BusFail($"Use exactly {requiredPCs} PCs and 1 backbone, with no switch. Currently: {pcs} PCs, {backbones} backbones, {others} other devices.");
        var connected = new HashSet<int>();
        var pairs = new HashSet<(int, int)>();
        foreach (var edge in edges)
        {
            if (!byID.TryGetValue(edge.a, out var a) || !byID.TryGetValue(edge.b, out var b))
                return BusFail("A cable has an endpoint outside this activity. Remove it and reconnect.");
            if (edge.a == edge.b) return BusFail("A device cannot connect to itself.");
            var pair = edge.a < edge.b ? (edge.a, edge.b) : (edge.b, edge.a);
            if (!pairs.Add(pair)) return BusFail($"Duplicate cable between {a.label} and {b.label}. Keep only one.");
            if (edge.a != backboneID && edge.b != backboneID)
                return BusFail($"Remove the direct connection between {a.label} and {b.label}. Each PC must attach only to the shared backbone.");
            connected.Add(edge.a == backboneID ? edge.b : edge.a);
        }
        var missing = new List<string>();
        foreach (var node in nodes)
            if (node.type == NetworkNodeType.PC && !connected.Contains(node.id)) missing.Add(node.label);
        if (missing.Count > 0) return BusFail("Connect these PCs to the backbone: " + string.Join(", ", missing) + ".");
        return new Result(true, $"BUS TOPOLOGY COMPLETE\n{requiredPCs} PCs attached to one shared backbone, with no extra connections.");
    }

    private static Result BusFail(string detail) => new Result(false, "BUS TOPOLOGY INCOMPLETE\n" + detail);
    private static Result RingFail(string detail) => new Result(false, "RING TOPOLOGY INCOMPLETE\n" + detail);
    private static Result Fail(string detail) => new Result(false, "STAR TOPOLOGY INCOMPLETE\n" + detail);
}
