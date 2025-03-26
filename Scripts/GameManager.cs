using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;

public partial class GameManager : Node2D
{
	Board chess_board;
	
    public enum Turn {
        White = -1,
        Black = 1
    }
	public enum GameState{
		White_win,
		Black_win,
		DrawNoMaterial,
		Stalemate,
		Ongoing
	}
    
	private Turn current_turn = Turn.White;
	private GameState state = GameState.Ongoing; 

	// Stats
	int total_plies = 0; // moves = total_plies/ 2
	float time_secs = 0; // keep track of time 

	public Dictionary<int, List<Piece>> prf;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Board board = new Board();
		board.gm = this;
		AddChild(board);
		
		chess_board = board; // sets the board ref
		style_pieces((int) current_turn);
		prf = board.PieceRefs; // get the piece refs for easy access

		Player player= new Player(board);
		AddChild(player);

	}

	// Called when a move is made
	public void Update(bool incmoves = false) {
		total_plies += Convert.ToInt32(incmoves); // add moves

		// Draw no material? Check possible states, then End the game if posisble
		if (prf[-1].Count + prf[1].Count <= 4) {
			// king vs king
			if (prf[-1].Count == 1 && prf[1].Count == 1) { set_state(GameState.DrawNoMaterial);}
			// king vs king + bishop
			if ((prf[-1].Count == 1 && !prf_contains(new string[2]{"b", "k"}, 1, prf)) || 
			(prf[1].Count == 1 && !prf_contains(new string[2]{"B", "K"}, -1, prf))) { set_state(GameState.DrawNoMaterial);}
			// king vs king + knight either side			
			if ((prf[-1].Count == 1 && !prf_contains(new string[2]{"k", "n"}, 1, prf)) || 
			(prf[1].Count == 1 && !prf_contains(new string[2]{"K", "N"}, -1, prf))) { set_state(GameState.DrawNoMaterial);}
			// king + bishop vs king + bishop if bishop are both on same color tile,
			if (prf[-1].Count == prf[1].Count 
			&& draw_get_bishop_square(1, prf) == draw_get_bishop_square(-1, prf) &&
			 draw_get_bishop_square(1, prf) != -1 ) { set_state(GameState.DrawNoMaterial);}
		}
		
		// Stalemate, White K
		
		
	}


	// HELPER METHOD for Update()
	/* Returns whether the bishop inside the prf of the id is on a white or black tile, using
		the property that row + column mod 2 is 1 for black tiles, and 0 for white tiles
	*/
	private int draw_get_bishop_square(int id, Dictionary<int, List<Piece>> prf) {
		foreach (Piece p in prf[id]) {
			if (p.get_piece_type() == Piece.PieceType.Bishop) {
				return (p.get_board_position().Item1 + p.get_board_position().Item2) % 2;
			}
		} 
		return -1;
	} 

	// Helper Method for Update()
	/*	Returns whether the prf contains the exact FEN given
		of the provided piece color id
	*/
	private bool prf_contains(string[] FEN, int id, Dictionary<int, List<Piece>> prf) {
		if (FEN.Length != prf[id].Count) {return false;} // not equal length, cant be equal
		// no index check necessary here since they are equal by virtue of the above
		foreach(Piece p in prf[id]) {
			if (!FEN.Contains(p.ToString())) {return false;}
		}
		return true; 
	}

	// get/set methods //


	// sets the state to the given GameManager.GameState
	public void set_state(GameState state) {
		this.state = state;
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
