# AntiDebug

Build in Visual Studio (.net 3.5 so it runs on Win 7+). 

This will spawn a watcher process to monitor a parent, and each will `pinvoke` a debugger thread on each other. If either process is killed (parent or child), they both exit, protecting each other from being debugged.

This is based on the (now old) concepts written here (click below to read details):
http://csharptest.net/1051/managed-anti-debugging-how-to-prevent-users-from-attaching-a-debugger/index.html

But with a full implementation and a refactor in more usable form.
