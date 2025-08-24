
// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Model;
// class GraphBuilder
// {
//     private Dictionary<int, Item> items;
//     private Dictionary<int, List<Edge>> adjacencyList = new Dictionary<int, List<Edge>>();

//     private Stack<ChainNode> startingNodes = new Stack<ChainNode>();

//     public GraphBuilder(List<Item> itemList, List<Item> startingItems, int maxChain)
//     {
//         items = itemList.OrderBy(i => i.Date).ToDictionary(i => i.Id);
//         BuildGraph(items.Values.ToList(), startingItems, maxChain);
//     }

//     private void BuildGraph(List<Item> itemList, List<Item> startingItems, int maxChain)
//     {
//         var tagGroups = itemList.SelectMany(i => i.Tags.Select(tag => (tag, item: i)))
//                                 .GroupBy(t => t.tag, t => t.item);


//         foreach (var group in tagGroups)
//         {
//             var itemsWithTag = group.OrderBy(i => i.Date).ToList();


//             foreach (var item in itemsWithTag)
//             {
//                 if (!adjacencyList.ContainsKey(item.Id))
//                 {
//                     adjacencyList[item.Id] = new List<Edge>();
//                 }
//             }

//             for (int i = 0; i < itemsWithTag.Count; i++)
//             {
//                 for (int j = i + 1; j < itemsWithTag.Count; j++)
//                 {
//                     adjacencyList[itemsWithTag[i].Id].Add(new Edge(itemsWithTag[i], itemsWithTag[j], group.Key, maxChain));
//                 }
//             }
//         }

//         foreach (var kvp in adjacencyList)
//         {
//             items[kvp.Key].AddEdgesOut(kvp.Value);
//         }
//         foreach (Item item in startingItems)
//         {
//             startingNodes.Push(new ChainNode(item));
//         }
//     }



//     public List<ChainNode> FindChain(int maxLength)
//     {
//         var chainsFound = new ConcurrentBag<ChainNode>(); // Thread-safe collection
//         var shareChains = new ConcurrentStack<ChainNode>(); // Thread-safe stack
//         int maxChain = 0;
//         int activeWorkers = 0;
//         int workRemaining = 1;

//         int workerCount = Environment.ProcessorCount; // Get the number of available CPU cores
//         List<Task> tasks = new List<Task>();
//         maxLength = 40;
//         while (startingNodes.TryPop(out ChainNode startingNode))
//         {
//             Console.WriteLine($"Starting node id {startingNode.CurrentNode.Id} title {startingNode.CurrentNode.Title}");
//             shareChains.Push(startingNode);

//             for (int i = 0; i < workerCount; i++) // Spawn worker threads
//             {
//                 tasks.Add(Task.Run(() =>
//                 {
//                     while (workRemaining == 1)
//                     {
//                         Interlocked.Increment(ref activeWorkers);
//                         if (shareChains.TryPop(out ChainNode currentNode)) // Each thread pops items independently
//                         {
//                             int currentMax;
//                             do
//                             {
//                                 currentMax = maxChain;
//                             } while (currentNode.ChainSize > currentMax &&
//                                     Interlocked.CompareExchange(ref maxChain, currentNode.ChainSize, currentMax) != currentMax);

//                             if (currentNode.ChainSize + 1 >= maxLength)
//                             {
//                                 foreach (Edge edge in currentNode.ConnectedTo)
//                                 {
//                                     chainsFound.Add(new ChainNode(edge.Destination, currentNode, edge));
//                                 }
//                             }
//                             else
//                             {
//                                 foreach (Edge edge in currentNode.ConnectedTo)
//                                 {
//                                     shareChains.Push(new ChainNode(edge.Destination, currentNode, edge)); // Dynamically adds items
//                                 }
//                             }
//                         }
//                         else
//                         {
//                             Interlocked.Decrement(ref activeWorkers);
//                             Thread.Sleep(50); // Brief pause to reduce CPU stress

//                             if (Interlocked.CompareExchange(ref activeWorkers, 0, 0) == 0 && shareChains.IsEmpty)
//                             {
//                                 Interlocked.Exchange(ref workRemaining, 0); // Safe termination trigger
//                             }
//                         }
//                         Interlocked.Decrement(ref activeWorkers);
//                     }
//                 }));
//             }
//             Task.WhenAll(tasks).Wait();
//             Console.WriteLine("");

//         }

//         Task.WaitAll(tasks.ToArray()); // Wait for all tasks to finish

//         return chainsFound.ToList();
//     }
//     public List<ChainNode> FindChainImproved(int maxLength)
//     {
//         var chainsFound = new ConcurrentBag<ChainNode>();
//         var sharedChains = new ConcurrentStack<ChainNode>();
//         int workerCount = Environment.ProcessorCount;
//         var semaphore = new SemaphoreSlim(0, workerCount);
//         ManualResetEvent workAvailable = new ManualResetEvent(false);
//         ManualResetEvent idlethread = new ManualResetEvent(false);
//         semaphore.Release(workerCount);
//         int activeWorkers = workerCount;
//         bool done = false;
//         int maxChain = 1;
//         maxLength = 30;
//         while (startingNodes.TryPop(out ChainNode startingNode))
//         {
//             Console.WriteLine($"Starting node id {startingNode.CurrentNode.Id} title {startingNode.CurrentNode.Title}");
//             sharedChains.Push(startingNode);

//             List<Task> tasks = new List<Task>();
//             for (int i = 0; i < workerCount; i++)
//             {
//                 tasks.Add(Task.Run(() =>
//                 {
//                     Console.WriteLine($"thread {Task.CurrentId} started");
//                     Stack<ChainNode> localChains = new Stack<ChainNode>();
//                     if (sharedChains.TryPop(out ChainNode startingNode))
//                     {
//                         localChains.Push(startingNode);        
//                     }
//                     while (true)
//                     {
//                         int currentMax;
//                         while (localChains.TryPop(out ChainNode currentNode))
//                         {
//                             do
//                             {
//                                 currentMax = maxChain;
//                             } while (currentNode.ChainSize > currentMax &&
//                                     Interlocked.CompareExchange(ref maxChain, currentNode.ChainSize, currentMax) != currentMax);

//                             if (currentNode.ChainSize + 1 >= maxLength)
//                             {
//                                 foreach (Edge edge in currentNode.ConnectedTo)
//                                 {
//                                     chainsFound.Add(new ChainNode(edge.Destination, currentNode, edge));
//                                 }
//                             }
//                             else
//                             {
//                                 foreach (Edge edge in currentNode.ConnectedTo)
//                                 {
//                                     localChains.Push(new ChainNode(edge.Destination, currentNode, edge));
//                                 }

//                             }
//                             //.waitOne(0) on a AutoResetEvent doesn't wait, it returns true if setted and reset
//                             // otherwise returns false
//                             if (idlethread.WaitOne(0))
//                             {
//                                 Console.WriteLine($"Taks id: {Task.CurrentId} filled sharedstack with {localChains.Count}");
//                                 sharedChains.PushRange(localChains.ToArray());
//                                 Console.WriteLine($"sharedtask with {sharedChains.Count} tasks");
//                                 workAvailable.Set();
//                                 idlethread.Reset();
//                                 workAvailable.Reset();
//                             }

//                         }
//                         //try to fill localstack
//                         if (sharedChains.TryPop(out startingNode))
//                         {
//                             localChains.Push(startingNode);
//                         }
//                         else
//                         {
//                             // check there is no more tasks and exit
//                             Interlocked.Decrement(ref activeWorkers);
//                             if (Volatile.Read(ref activeWorkers) == 0 && sharedChains.IsEmpty)
//                             {
//                                 // false workavailable signal to stop idlethreads from waiting
//                                 // when no more tasks are available
//                                 workAvailable.Set();
//                                 break;
//                             }        

//                             // signal idlethread and wait for tasks to be available
//                             idlethread.Set();
//                             Console.WriteLine($"task {Task.CurrentId} is idle");
//                             workAvailable.WaitOne();
//                             if (sharedChains.TryPop(out startingNode))
//                             {
//                                 localChains.Push(startingNode);
//                                 Interlocked.Increment(ref activeWorkers);
//                                 Console.WriteLine($"task {Task.CurrentId} started running");
//                             }
//                             else
//                             {
//                                 break;
//                             }
//                         }
//                     }
//                 }));
//             }
            

//             Task.WaitAll(tasks.ToArray());
//         }


//         return chainsFound.ToList();
//     }





//     public List<ChainNode> UpdateEdgeMax(int maxLength)
//     {
//         var chainsFound = new ConcurrentBag<ChainNode>();
//         var sharedStack = new ConcurrentStack<ChainNode>();
//         int workerCount = Environment.ProcessorCount;
//         ManualResetEvent workAvailable = new ManualResetEvent(false);
//         int activeWorkers = workerCount;
//         int maxChain;
//         int maxGlobalChain = 0;
//         maxLength = 40;
//         List<Edge> edges = new List<Edge>();
//         edges = adjacencyList.SelectMany(e => e.Value).OrderByDescending(e => e.Source.Date).ToList();
//         foreach (Edge startEdge in edges)
//         {
//             maxChain = 1;
//             activeWorkers = workerCount;
//             Console.WriteLine($"Starting edge: {startEdge.Source.Id} --> {startEdge.Destination.Id} tag: {startEdge.Tag}");
//             sharedStack.Push(new ChainNode(startEdge.Destination, new ChainNode(startEdge.Source), startEdge));

//             List<Task> tasks = new List<Task>();
//             for (int i = 0; i < workerCount; i++)
//             {
//                 tasks.Add(Task.Run(() =>
//                 {
//                     Console.WriteLine($"thread {Task.CurrentId} started");
//                     int currentMax;
//                     Stack<ChainNode> localStack = new Stack<ChainNode>();
//                     List<Edge> nextEdges = new List<Edge>();
//                     if (sharedStack.TryPop(out ChainNode startingNode))
//                     {
//                         localStack.Push(startingNode);
//                     }
//                     while (true)
//                     {

//                         while (localStack.TryPop(out ChainNode currentNode))
//                         {
//                             nextEdges = currentNode.ConnectedTo
//                                             .Where(e => e.MaxChainSize >= (maxChain - currentNode.ChainSize)).ToList();

//                             if (nextEdges.Any())
//                             {
//                                 if (currentNode.ChainSize + 1 > maxChain)
//                                 {
//                                     do
//                                     {
//                                         currentMax = maxChain;
//                                     } while (currentNode.ChainSize + 1 > currentMax &&
//                                             Interlocked.CompareExchange(ref maxChain, currentNode.ChainSize, currentMax) != currentMax);
//                                 }

//                                 if (currentNode.ChainSize + 1 >= maxLength)
//                                 {
//                                     foreach (Edge edge in currentNode.ConnectedTo)
//                                     {
//                                         chainsFound.Add(new ChainNode(edge.Destination, currentNode, edge));
//                                     }
//                                 }
//                                 else
//                                 {
//                                     foreach (Edge edge in currentNode.ConnectedTo)
//                                     {
//                                         localStack.Push(new ChainNode(edge.Destination, currentNode, edge));
//                                     }
//                                 }
//                             }



//                            // otherwise returns false
//                             if (Volatile.Read(ref activeWorkers) < workerCount && localStack.Count > 1 && sharedStack.IsEmpty)
//                             {
//                                 sharedStack.PushRange(localStack.ToArray());
//                                 workAvailable.Set();
//                                 Console.WriteLine($"Taks id: {Task.CurrentId} filled sharedstack with {localStack.Count}");
//                                 localStack.Clear();
//                                 if (sharedStack.TryPop(out startingNode))
//                                 {
//                                     localStack.Push(startingNode);
//                                 }
//                             }

//                         }
//                         //try to fill localstack
//                         if (sharedStack.TryPop(out startingNode))
//                         {
//                             localStack.Push(startingNode);
//                         }
//                         else
//                         {
                            
                            
//                             Interlocked.Decrement(ref activeWorkers);
//                             // check there is no more tasks and exit
//                             if (Volatile.Read(ref activeWorkers) == 0 && sharedStack.IsEmpty)
//                             {
//                                 // false workavailable signal to stop idlethreads from waiting
//                                 // when no more tasks are available
//                                 workAvailable.Set();
//                                 break;
//                             }
//                             Console.WriteLine($"task {Task.CurrentId} is idle");
//                             workAvailable.WaitOne();
//                             Interlocked.Increment(ref activeWorkers);
//                             if (sharedStack.TryPop(out startingNode))
//                             {
//                                 localStack.Push(startingNode);
//                                 Console.WriteLine($"task {Task.CurrentId} started running");
//                             }
//                             else if (Volatile.Read(ref activeWorkers) == 0 && sharedStack.IsEmpty)
//                             {
//                                 Interlocked.Decrement(ref activeWorkers);
//                                 break;
//                             } else
//                             {
//                                 workAvailable.Reset();
//                             }
//                         }
//                     }
//                     if (!sharedStack.IsEmpty)
//                     {
//                         Console.WriteLine("FUCK!!!!!");
//                     }
//                 }));
//             }



//             Task.WaitAll(tasks.ToArray());
//             Console.WriteLine($"Edge {startEdge.Source.Id} --> {startEdge.Destination.Id} tag: {startEdge.Tag} new Maxchain {maxChain}");
//             startEdge.UpDateMax(maxChain);
//             if (maxChain > maxGlobalChain)
//             {
//                 maxGlobalChain = maxChain;
//             }
//         }


//         return chainsFound.ToList();
//     }
//     private bool DFS(Item node, int maxLength, List<Item> chain, HashSet<string> usedTags)
//     {
//         if (chain.Count == maxLength)
//             return true;

//         chain.Add(node);
//         var validEdges = adjacencyList[node.Id]
//             .Where(e => !usedTags.Contains(e.Tag))
//             .OrderBy(e => e.Destination.Date);

//         foreach (var edge in validEdges)
//         {
//             var next = edge.Destination;
//             usedTags.Add(edge.Tag);

//             if (DFS(next, maxLength, chain, usedTags))
//                 return true;

//             usedTags.Remove(edge.Tag); // Backtrack
//         }

//         chain.Remove(node); // Backtrack
//         return false;
//     }
// }