# Task-like Async Enumerators
## _a.k.a Abusing Task-like Types in C# 7_

Since async/await was first released many have craved a corresponding 'async iterator' approach that could blend both the yield and async syntaxes.

While there are several options currently available for asynchronous sequences (Rx Observables, DataFlow blocks), none have the concise beauty of async/await and yield iterators. 

Async sequences are on the book for C# 8, and there are a number of [interesting discussions](https://github.com/dotnet/roslyn/issues/261) happening in the Rosyln issues.

But why wait - with C# 7's new Task-Like types we finally have the possibility to create async methods that return custom types. By capturing the underlying task-like object from within the method itself, we can return values _before_ the task has completed.

Mads Torgersen has repeated that very few people will create Task-Like types, but I believe the approach shown here will have many cool applications. This repository contains my initial proof of concept.

> ### Warning! 
> I put this together as an experiment, there are likely better ways to approach most of the inner workings. I put zero emphasis on correctness, safety, or performance.

### Details

This repository contains several Task-like types that allow both cooporative and parallel async iterator methods. 

#### AsyncEnumerator&lt;T&gt;

The `AsyncEnumerator<T>` class provides behavior similar to a standard yield iterator method except that it allows for asynchronous operations. Each `yield.Return(T)` call returns a value and asynchronously waits for the next call to `MoveNextAsync()`:

``````````` c#

public static async AsyncEnumerator<int> Producer()
{
    var yield = await AsyncEnumerator<int>.Capture(); // Capture the underlying 'Task'

    await yield.Pause();             // Optionally wait for the first MoveNext call

    await yield.Return(1);           // Yield the value and await MoveNext
                   
    await Task.Delay(100);           // Use any async constructs

    await yield.Return(2);

    return yield.Break();            // Return false to awaiting MoveNext
}

public static async Task Consumer()
{
    var p = Producer();                       

    while (await p.MoveNextAsync())       // Await the next value
    {
        Console.Write(" " + p.Current);   // Use the current value
    }
}

````````````
#### AsyncSequence&lt;T&gt;

While cooperative iteration is great, I believe the more common desire with async code would be to have the producer run in parallel and simply await the availability of results (closer in concept to observables). The `AsyncSequence<T>` class shown below accomplishes this goal:

``````````` c#

public static async AsyncSequence<int> Producer2()
{
    var seq = await AsyncSequence<int>.Capture();            // Capture the underlying 'Task'
                       
    var users = await GetUsersAsync().ConfigureAwait(false); // Use any async constructs
    
    foreach(var user in users)
    {
        var fiends = await user.GetFriendsAsync(); // Build async 'flows' naturally 
        
        seq.Return(friends.Count);            // Signal an awaiting 
    }                                         // MoveNext, or queue the result.

    return seq.Break();                       // Complete the sequence and 
}                                             // return false to an awaiting MoveNext

public static async Task Consumer2()
{
    var p = Producer2();

    while (await p.MoveNextAsync())     // Await the next value
    {
        Console.WriteLine(p.Current);   // Use the current value
    }
}

```````````````

#### Other Types

I also threw together several additional types with a similar approach. One is a `TaskLikeObservable` which allows you to write a _flat_ and async `IObservable` method such as:

`````````````` c#

public static async TaskLikeObservable<string> Producer()
{
    var o = await TaskLikeObservable<string>.Capture(); // Capture the underlying Task-like Obserable

    await o.Subscription;                               // wait for a subscriber

    for (var i = 0; i < 10; i++)
    {
        await Task.Delay(100).ConfigureAwait(false);    // Use normal async constructs
        
        o.OnNext("y" + i);                              // send the value
    }

    return o.OnCompleted();                             // complete the observable and return.
}

...

Producer().Subscribe(i => DoSomethingWith(i));          // Run and subscribe to the method

```````````````

Lastly I threw in a `CoopTask` class which allows a parent and child task to pass control back and forth similarly to a yielding enumerator:

``````````` c#

public static async CoopTask Child()
{
    var task = await CoopTask.Capture();          // Capture the underlying 'Task'

    Console.WriteLine("P0");

    await task.Yield();                           // Yield control back to parent

    await Task.Delay(100).ConfigureAwait(false);  // Use any async constructs

    Console.WriteLine("P1");

    await task.Yield();                           // Yield control

    await Task.Delay(100);

    Console.WriteLine("P2");

    await task.Break();                           // Mark the task as completed

    Console.WriteLine("P3");                      // Will not be run.
}

public static async Task Parent()
{
    var p = Child();

    var i = 1;

    while (await p.MoveNextAsync())          // Await the next child operation
    {
        Console.WriteLine("C" + i++);        
    }
}

````````````

## Discussion

There are likely better ways of implementing most of the internals of these types. For one thing, I largely used classes over structs (which are used by the standard framework types) to allow for more code reuse.

The 'hack' for capturing the underlying Task-like object is not as bad as I thought it would have to be - I originally used reflection,  then found the current method which only relies on a 'dummy' continuation.

The main oddity I have run into is that the compiler appears to treat all generic Task-like methods as if they should behave like `Task<T>` even when their builder semantics actually follow the void returning `Task` approach. This means you have to 'return' a value even when the _Task_ and awaiter have no `Result` field. I guess this wasn't the intended usage...

I will be very curious to hear other developer's feedback. Please leave comments or feedback as issues. And of course, I welcome any pull requests for better code or more functionality.







