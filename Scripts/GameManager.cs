using Godot;
using System;

public partial class GameManager : Node2D
{
	Board chess_board;
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Board board = new Board();
		board.gm = this;
		AddChild(board);

		Player player= new Player(board);
		AddChild(player);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
