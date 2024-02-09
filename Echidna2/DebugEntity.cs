﻿using Echidna2.Core;

namespace Echidna2;

public class DebugEntity : IUpdate
{
	public void Update(double deltaTime)
	{
		Console.WriteLine($"Updating {deltaTime}");
	}
}