using Godot;
using System;

public partial class GameManager : Node2D
{
	Board chess_board;
	
    public enum Turn {
        White = -1,
        Black = 1
    }

	private Turn current_turn = Turn.White;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Board board = new Board();
		board.gm = this;
		AddChild(board);
		
		chess_board = board; // sets the board ref
		style_pieces((int) current_turn);

		Player player= new Player(board);
		AddChild(player);

		
		
	}

	// returnns the current turn
    public Turn get_current_turn() {
		return current_turn;
	}

	// swaps current turn to the other person
	public void alternate_turn() {
		if (get_current_turn() == Turn.White) {current_turn = Turn.Black;}
		else if (get_current_turn() == Turn.Black) {current_turn = Turn.White;}

		style_pieces((int) current_turn);
	}

	// styles the pieces of the given color
	public void style_pieces(int color) {
		foreach (Piece p in chess_board.PieceRefs[-color]) {
			p.Modulate = new Color(1f, 1f, 1f, 0.3f);
		}
		foreach (Piece p in chess_board.PieceRefs[color]) {
			p.Modulate = new Color(1f, 1f, 1f, 1f);
		}
	}
}
