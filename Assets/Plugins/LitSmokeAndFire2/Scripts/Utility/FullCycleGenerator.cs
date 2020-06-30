using UnityEngine;
using System.Collections;

//this code generates a psudo random number between 0 and n and does not repeat
[System.Serializable]
public class FullCycleGenerator 
{

	public int seed;
	public int n;
	public int prime;
	public int curr;
		
	public bool _bIsSetup = false;

		public void Setup(int seed, int n)
		{
			this.seed = seed; // don't really need to store
			this.n = n;
			this.prime = GetPrime((int)(n *0.1f));
			if (this.prime == -1) {
				} else {
						this.curr = seed % n; // start at random location in cycle
			_bIsSetup = true;
				}
		}
		
		private static int GetPrime(int n)
		{

			// first prime p where p > n/3 && p < n &&
			// p does not divide into n evenly
			int p = (n / 3) + 1;
			while (p < n)
			{
				if (IsPrime(p) && n % p != 0)
					return p;
				++p;
			}
			return -1; // error
		}
		
		private static bool IsPrime(int n) // helper for GetPrime
		{
			int divisor = 2;
			int maxDivisor = (int)(System.Math.Sqrt(n) + 1);
			
			while (divisor < maxDivisor)
			{
				if (n % divisor == 0)
					return false;
				++divisor;
			}
			return true;
		}
		
		public int NextInt() // next int in the full cycle
		{
		if (_bIsSetup == false) {
			Debug.Log("Not Set Up");
						return 0;
				}

			//this.curr = (this.curr + this.prime) % this.n;  // risky
			this.curr = ModuloOfSum(this.curr, this.prime, this.n);
			return this.curr;
		}
		
		private static int ModuloOfSum(int a, int b, int m)
		{
			// return (a + b) mod m
			int mod1 = a % m;
			int mod2 = b % m;
			return (mod1 + mod2) % m;
		}
		
} // FullCycle
