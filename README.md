# Task-like Async Enumerators
## _a.k.a Abusing Task-like Types in C# 7_

Since async/await was first released I have longed for 'async iterator' methods that could combine both the yield and async syntaxes.

While there are several solutions available for asynchronous sequences (Rx Observables, DataFlow blocks), none have the concise beauty of async/await and yield iterators.

With C# 7's new Task-Like types, we finally have the possibility to create an async method that returns a custom type. By capturing the underlying task-like object from within the method itself, we can return values _before_ the task has completed.

I believe this approach will have many cool applications. This repository contains my initial proof of concept.

</br>

> ### Warning! 
> I put this together as an experiment, there are likely better ways to approach most of the inner workings. I don't recommend this for production code!

</br>

### Details

This repository contains several Task-like types that allow both cooporative and parallel iterator methods. 

The `AsyncEnumerator<T>` class provides behavior similar to a standard yield iterator method except that it allows for asynchronous operations. Each `Yield(T)` call returns a value and asynchronously waits for the next call to `MoveNext()`:

``````````` c#

public static async AsyncEnumerator<int> Producer()
{
    var e = await AsyncEnumerator<int>.Capture(); // Capture the underlying 'Task'

    await e.YieldInit();                          // Optionally wait for first MoveNext call

    await e.Yield(1);                             // Yield the value and wait for MoveNext
                   
    await Task.Delay(100);                        // Use any async constructs

    await e.Yield(2);

    return e.YieldReturn();                       // Return false to awaiting MoveNext
}

public static async Task Consumer()
{
    var p = Producer();                       

    while (await p.MoveNext())                // Await the next value
    {
        Console.WriteLine(" " + p.Current);   // Use the current value
    }
}

````````````

While cooperative iteration is great, the more common desire with async code (I think) would be to have the producer run in its own thread and simply await the availability of results (closer in concept to observables). The `AsyncParallelEnumerator<T>` shown below does exactly this:

``````````` c#

public static async AsyncParallelEnumerator<int> Producer2()
{
    var e = await AsyncParallelEnumerator<int>.Capture();    // Capture the underlying 'Task'
                       
    var users = await GetUsersAsync().ConfigureAwait(false); // Use any async constructs
    
    foreach(var user in users)
    {
        var fiends = await user.GetFriendsAsync(); 
        
        e.Yield(friends.Count);                   // Yield values without waiting for MoveNext
    }

    return e.YieldReturn();                       // Complete the sequence and 
}                                                 // return false to an awaiting MoveNext

public static async Task Consumer2()
{
    var p = Producer2();

    while (await p.MoveNext())          // Await the next value
    {
        Console.WriteLine(p.Current);   // Use the current value
    }
}

Output: 1 2

```````````````

I also threw together several additional types with a similar approach. One is a `TaskLikeObservable` which allows you to write a _flat_ `IObservable` method such as:

`````````````` c#

public static async TaskLikeObservable<string> Producer()
{
    var e = await TaskLikeObservable<string>.Capture(); // Capture the underlying Task

    await e.Subscription;                               // wait for a subscriber

    for (var i = 0; i < 10; i++)
    {
        await Task.Delay(100).ConfigureAwait(false);    // Use normal async constructs
        
        e.OnNext("y" + i);                              // send the value
    }

    return e.OnCompleted();                             // complete the observable and return.
}

...

Producer().Subscribe(i => DoSomethingWith(i));          // Run and subscribe to the method

```````````````



## Discussion

There are likely far more clever ways of implementing most of the internals of these types. The 'hack' for capturing the underlying Task-like object is not as bad as I thought it would have to be - it originally used reflection - but the current method still relies on a 'dummy' continuation.

The main oddity I have run into is that the compiler appears to treat all generic Task-like methods as if they should behave like `Task<T>` even when their builder semantics actually follow the void returning `Task` approach. This means you have to 'return' a value even though the Task has no Result value. I will be posting an issue on the Roslyn repository.









