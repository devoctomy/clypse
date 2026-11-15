// The scenarios share one test user and their one test vault, and each depends on what the ones before it left: the
// first creates the test vault, the next use it. They must never run at the same time.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]
