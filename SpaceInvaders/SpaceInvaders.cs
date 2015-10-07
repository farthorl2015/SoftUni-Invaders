﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

class SpaceInvaders
{
	// Variables used in the methods.
	const int MaxHeight = 30;
	const int MaxWidth = 70;
	const int FieldWidth = MaxWidth / 6;

	static int playerPositionX = FieldWidth / 2;
	static int playerPositionY = MaxHeight - 2;

	static List<int[]> enemies = new List<int[]>();
	static List<int[]> shots = new List<int[]>();

	const char PlayerSymbol = 'W';
	const char EnemySymbol = '@';
	const char ShotSymbol = '|';

	const ConsoleColor Blue = ConsoleColor.Blue;

	//Level details
	static int pauseDivider = 16; //changing count of enemies depending on level;
	static int lives = 3;
	static int pause; // Adjustment of enemies being spawned
	static int winnedScoresInLevel; // counting points at each level
	static int scoresToWin = 10; // the count of scores that are needed to go to next level
	static int level;
	static int numberOfLevels = 3;

	static Random generator = new Random(); // this is the generator for the starting position of the enemies.

	static bool wonLevel;
	static int timestep = 90; // Timestep in milliseconds. Determines how fast the enemies fall down.
	static bool frozenUsed;
	static bool enemiesAreFrozen;


	static void Main()
	{
		Console.Title = "!---> Space Invaders <---!";
		Console.BufferHeight = Console.WindowHeight = MaxHeight;
		Console.BufferWidth = Console.WindowWidth = MaxWidth;

		PlayingLevel();
	}

	static void PlayingLevel()
	{
		Stopwatch syncTimer = Stopwatch.StartNew(); // timestep stopwatch

		while (lives > 0)
		{
			ReadPlayerMovement();

			if (syncTimer.ElapsedMilliseconds % (timestep - (level*5)) == 0) // difficulty rises as level rises
			{
				// Draw
				SpawnEnemies(enemiesAreFrozen);
				DrawField();

				// Logic

				UpdateShotPosition();
				Collision();

				if (!enemiesAreFrozen)
				{
					UpdatingEnemyPosition();
					Collision();
				}
				//Thread.Sleep(sleepTime);
				Console.Clear();

				// Redrawing
				DrawField();
			}
			LevelStatus();
		}

		WriteLabel(new StreamReader(@"..\..\msg_game_over.txt"));

		Console.ReadLine();
		Environment.Exit(0);
	}

	static void LevelStatus()
	{
		wonLevel = winnedScoresInLevel >= scoresToWin;

		if (wonLevel)
		{
			level++;
			GoToNextLevel();
		}
	}

	static void ReadPlayerMovement()
	{
		while (Console.KeyAvailable)
		{
			var keyPressed = Console.ReadKey(true);

			while (Console.KeyAvailable) // cleans read key buffer when a lot of keys are pressed
			{
				Console.ReadKey(true);
			}

			switch (keyPressed.Key)
			{
				case ConsoleKey.RightArrow:
				case ConsoleKey.D:

					if (playerPositionX < FieldWidth)
					{
						playerPositionX++;
					}
					break;

				case ConsoleKey.LeftArrow:
				case ConsoleKey.A:

					if (playerPositionX > 0)
					{
						playerPositionX--;
					}
					break;

				case ConsoleKey.DownArrow:
				case ConsoleKey.S:

					if (playerPositionY < MaxHeight - 2)
					{
						playerPositionY++;
					}
					break;

				case ConsoleKey.UpArrow:
				case ConsoleKey.W:

					if (playerPositionY > 1)
					{
						playerPositionY--;
					}
					break;

				case ConsoleKey.Spacebar:

					shots.Add(new[] { playerPositionX, playerPositionY });
					break;

				case ConsoleKey.NumPad0:

					if (!frozenUsed)
					{
						Thread freeze = new Thread(Freeze());
						freeze.Start();
					}

					frozenUsed = true;
					break;
			}
		}
	}

	static void GoToNextLevel()
	{

		if (level > numberOfLevels)
		{

			WriteLabel(new StreamReader(@"..\..\msg_you_won.txt"));

			Console.ReadLine();
			Environment.Exit(0);
		}

		PrintStringAtCoordinates(20, 12, Blue, "PRESS ENTER TO GO TO THE NEXT LEVEL");
		while (true)
		{
			var keyPressed = Console.ReadKey();

			if (keyPressed.Key == ConsoleKey.Enter)
			{
				Console.Clear();
				winnedScoresInLevel = 0;
				wonLevel = false;
				ConfigurateLevelDetails();
				break;
			}
		}
	}

	static void ConfigurateLevelDetails()
	{
		enemies.Clear();
		shots.Clear();
		playerPositionX = FieldWidth / 2;
		playerPositionY = MaxHeight - 2;
		pauseDivider -= 2;
		timestep -= 10;
		lives++;
	}

	static void DrawResultTable()
	{
		PrintStringAtCoordinates(20, 4, Blue, "SPACE INVADERS");
		PrintStringAtCoordinates(20, 6, Blue, string.Format("Lives: {0}", lives));
		PrintStringAtCoordinates(20, 7, Blue, string.Format("Level: {0}", level));
		PrintStringAtCoordinates(20, 8, Blue, string.Format("Next level after {0} enemies kills", scoresToWin - winnedScoresInLevel));

	}
	static void UpdateShotPosition()
	{
		shots.ForEach(shot => shot[1]--);
	}

	static void UpdatingEnemyPosition()
	{
		enemies.ForEach(enemy => enemy[1]++);
	}

	static void Collision()
	{
		List<int> enemiesToRemove = new List<int>();
		List<int> shotsToRemove = new List<int>();
		List<int[]> enemiesLeft = new List<int[]>();
		List<int[]> shotsLeft = new List<int[]>();

		EnemiesTakingLife(enemiesToRemove);
		EnemiesVsShots(enemiesToRemove, shotsToRemove);
		UpdatingTheEnemiesList(enemiesLeft, enemiesToRemove); // here we're getting the new list of enemies after the collision
		UpdatingTheShotsList(shotsLeft, shotsToRemove);
		shots = shotsLeft;
		enemies = enemiesLeft;
	}

	static void UpdatingTheShotsList(List<int[]> shotsLeft, List<int> shotsToRemove)
	{
		for (int i = 0; i < shots.Count; i++)
		{
			if (shotsToRemove.Contains(i))
			{
				continue;
			}
			if (shots[i][1] < 1)
			{
				continue;
			}
			shotsLeft.Add(shots[i]);
		}
	}

	static void UpdatingTheEnemiesList(List<int[]> enemiesLeft, List<int> enemiesToRemove)
	{
		for (int i = 0; i < enemies.Count; i++)
		{
			if (enemiesToRemove.Contains(i))
			{
				continue;
			}
			enemiesLeft.Add(enemies[i]);
		}
	}

	static void EnemiesVsShots(List<int> enemiesToRemove, List<int> shotsToRemove)
	{
		for (int i = 0; i < shots.Count; i++)
		{
			int theEnemyCollidedWithAShot = enemies.FindIndex(enemy => enemy[0] == shots[i][0] && enemy[1] == shots[i][1]);
			if (theEnemyCollidedWithAShot >= 0)
			{
				enemiesToRemove.Add(theEnemyCollidedWithAShot);
				shotsToRemove.Add(i);
				winnedScoresInLevel++;
			}

		}
	}

	static void EnemiesTakingLife(List<int> enemiesToRemove)
	{
		for (int index = 0; index < enemies.Count; index++)
		{
			if ((enemies[index][0] == playerPositionX && enemies[index][1] == playerPositionY) || enemies[index][1] >= MaxHeight - 2)
			{
				lives--;
				DrawAtCoordinates(new[] { enemies[index][0], enemies[index][1] }, ConsoleColor.DarkRed, 'X');
				enemiesToRemove.Add(index);
			}

		}
	}




	static void FieldBarrier()
	{
		for (int i = 1; i < MaxHeight - 2; i++)
		{
			DrawAtCoordinates(new[] { FieldWidth + 1, i }, Blue, '|');
		}
	}

	static void DrawField()
	{
		DrawEnemies();
		DrawShots();
		DrawPlayer();
		DrawResultTable();
		FieldBarrier();
	}

	static void DrawPlayer()
	{
		int[] playerPosition = { playerPositionX, playerPositionY };
		ConsoleColor playerColor = Blue;
		DrawAtCoordinates(playerPosition, playerColor, PlayerSymbol);
	}

	static void DrawShots()
	{
		foreach (var shot in shots)
		{
			DrawAtCoordinates(new[] { shot[0], shot[1] }, ConsoleColor.Red, ShotSymbol);
		}
	}

	static void DrawEnemies()
	{
		foreach (var enemy in enemies)
		{
			DrawAtCoordinates(new[] { enemy[0], enemy[1] }, ConsoleColor.Red, EnemySymbol);
		}
	}
	static void DrawAtCoordinates(int[] objectPosition, ConsoleColor objectColor, char objectSymbol)
	{
		Console.SetCursorPosition(objectPosition[0], objectPosition[1]);
		Console.ForegroundColor = objectColor;
		Console.WriteLine(objectSymbol);
		Console.CursorVisible = false;
	}

	static void PrintStringAtCoordinates(int stringPositionX, int stringPositionY, ConsoleColor stringColor, string message)
	{
		Console.SetCursorPosition(stringPositionX, stringPositionY);
		Console.ForegroundColor = stringColor;
		Console.WriteLine(message);
		Console.CursorVisible = false;
	}
	static void SpawnEnemies(bool frozen)
	{
		if (frozen) return;

		if (pause % pauseDivider == 0)
		{
			int spawningWidth = generator.Next(0, FieldWidth);
			int spawningHeight = generator.Next(0, MaxHeight / 6);
			enemies.Add(new[] { spawningWidth, spawningHeight });
			pause = 0;
		}
		pause++;
	}
	static ThreadStart Freeze()
	{
		ThreadStart freeze = () =>
		{
			Stopwatch sb = new Stopwatch();
			const int millisecondsOfFreeze = 4000;
			sb.Start();
			while (sb.ElapsedMilliseconds < millisecondsOfFreeze)
			{
				enemiesAreFrozen = true;
			}
			enemiesAreFrozen = false;
		};
		return freeze;
	}

	static void WriteLabel(StreamReader file) //flag
	{
		Console.Clear();
		int y = MaxHeight / 2 - 5;
		int x = MaxWidth / 5;

		using (file)
		{
			while (true)
			{
				var line = file.ReadLine();
				if (string.IsNullOrEmpty(line))
				{
					break;
				}
				Console.SetCursorPosition(x, y);
				Console.WriteLine(line);

				y++;
			}
		}
	}
}