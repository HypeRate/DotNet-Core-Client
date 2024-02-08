HypeRateClient hyperateClient = new("MY TOKEN HERE");

await hyperateClient.Connect(CancellationToken.None);
hyperateClient.JoinChannel("internal-testing");
