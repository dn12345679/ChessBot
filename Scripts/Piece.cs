using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

public partial class Piece : Node2D
{
	// MODDING GUIDE:
	//	Add a PieceType to the enum
	//	in Board.cs, change the ReadForsythEdwards() function to accomdate the new letter
		// - also create a sprite so that it maps correctly
		// - also adjust here: functions related to is_pinned(), like get_enemies()
	// Read the instructions in MoveManager.cs
	// That should be all

	public enum PieceType
	{
		Default = 0, 
		Pawn = 1,
		Rook = 2, 
		Knight = 3, 
		Bishop = 4, 
		Queen = 5, 
		King = 6
	}
	public enum PieceColor
	{
		White = -1,
		Black = 1, 
		Default = 0
	}

	public enum State
	{
		Unmoved = -1, // for first move logic
		Placed = 0, // for everything else
		Picked = 1, // Idk
		Captured = 2, // ?

		Checked = 3, // Applies to King only. 
	}
	// board reference
	Board board; // important for checks and pins

	// piece stats
	private PieceType ptype = PieceType.Default;
	private PieceColor pcolor = PieceColor.Default;
	private Vector2 PiecePosition = Vector2.Zero; // set in Board.cs
	
	public PieceHistory phist = new PieceHistory(); // for move rollback

	private Tuple<int, int> PieceIndex = new Tuple<int, int>(0, 0); // for indexing array, in format

	private bool is_threatened = false; // for king checks, but also convenient for any other logic
	// piece state

	private State pstate = State.Unmoved; // initial state 

	private TextureRect image = null;
	public char rep = '_';
	
	public Piece(Vector2 pPos, int PieceType, int PieceColor, Board chess_board, char rep) {
		this.PiecePosition = pPos;

		foreach (PieceType pval in Enum.GetValues(typeof(PieceType))) {
			if ((int) pval == PieceType) {
				ptype = pval;
			}
		}
		pcolor = PieceColor == 1 ? Piece.PieceColor.White : Piece.PieceColor.Black;
		SetIcon(); // adds images to the piece
		GlobalPosition = pPos;
		board = chess_board;
		this.rep = rep;
	}


	// check if an enemy rook, queen is targetting horizontally/vertically
	// and likewise for bishop, queen is targetting diagonally
	// Returns true if there is a pin on Piece p, otherwise returns false
	// sorry future me for the nested conditions, its the best i could do
	// IMPORTANT: must modify if you are adding new pieces
	public Tuple<bool, Piece> is_pinned(Piece p) {

		// not even a piece, success
		if (p == null) {return new Tuple<bool, Piece>(false, null);}
		// set the king reference to the respective color
		Piece king = (p.get_piece_color() == (int) Piece.PieceColor.White) ? Board.White_King : Board.Black_King;

		// feasibility check
		int distX = king.get_board_position().Item2 - p.get_board_position().Item2;
		int distY = king.get_board_position().Item1 - p.get_board_position().Item1;
		// not same row, not same column, not same diagonal. Break early
		if (distX != 0 && distY != 0 && Math.Abs(distX) != Math.Abs(distY)) 
		{
			// not plausible, success
			return new Tuple<bool, Piece>(false, null);
		}
		// valid, get the direction
		if (distX != 0) { distX /= Math.Abs(distX);}
		if (distY != 0) { distY /= Math.Abs(distY);}

		Tuple<int, int> pos = p.get_board_position();
		for (int i = 0; i < 8; i++) {
			pos = new Tuple<int, int>(pos.Item1 + distY, pos.Item2 + distX);
			if (Move.tuple_in_bounds(pos)) {
				Piece currpiece = board.BoardTiles[pos.Item1, pos.Item2];
				if (currpiece != null ) {
					// valid piece found in the direction of the king, but it wasnt the king
					/* AND the piece is not in the captured state: NOTE that captured state means 
						it is still in the plane of existence, but ceases to interact with other pieces
					*/
					if (currpiece.get_piece_type() != Piece.PieceType.King && currpiece.get_state() != Piece.State.Captured) {
						// no king pin, success
						
						return new Tuple<bool, Piece>(false, null); // not a king, no possible way of pinning in the direction to the king
					}
					else{
						// trace in opposite direction
						pos = p.get_board_position(); // reset the position
						for (int j = 0; j < 8; j++) 
						{
							pos = new Tuple<int, int>(pos.Item1 - distY, pos.Item2 - distX);
							if (Move.tuple_in_bounds(pos)) {
								// currpiece is the current piece being iterated over, or null
								currpiece = board.BoardTiles[pos.Item1, pos.Item2];
								if (currpiece != null && currpiece.get_state() != Piece.State.Captured) {
									// valid piece found
									if (currpiece.get_piece_color() == p.get_piece_color()) {
										// no pin due to skin color, success
										return new Tuple<bool, Piece>(false, null); // not an opp, no possible way of pinning in the direction to the king
									}
									else {
										if (Math.Abs(distX) == Math.Abs(distY)) {
											if (currpiece.get_piece_type() == Piece.PieceType.Queen 
												|| currpiece.get_piece_type() == Piece.PieceType.Bishop) {
													
													return new Tuple<bool, Piece>(true, currpiece);
												}
											break;
										}
										else {
											
											if (currpiece.get_piece_type() == Piece.PieceType.Queen 
												|| currpiece.get_piece_type() == Piece.PieceType.Rook) {
													
													return new Tuple<bool, Piece>(true, currpiece);
												}
											break;
										}
									}
								}
							}
						}
					}

				}
			}
			else {
				// out of bounds, exist loop, return false by default
				break;
			}

		}

		// no pin, success
		return new Tuple<bool, Piece>(false, null);
	}

	// Returns a List of threats (Pieces) in all possible directions
	// in relation to the origin position (unrelated to piece type)
	public List<Piece> get_threats(Tuple<int, int> origin, Piece[,] board) {
		List<Piece> threats = new List<Piece>();

        Tuple<int, int>[] directions = new Tuple<int, int>[16] {
            new Tuple<int, int>(0, -1), new Tuple<int, int>(1, 0), new Tuple<int, int>(0, 1), new Tuple<int, int>(-1, 0),   // Horizontal/Vertical
            new Tuple<int, int>(-1, -1), new Tuple<int, int>(1, 1), new Tuple<int, int>(-1, 1), new Tuple<int, int>(1, -1),   // Diagonal
            new Tuple<int, int>(2, 1), new Tuple<int, int>(2, -1), new Tuple<int, int>(-2, 1), new Tuple<int, int>(-2, -1),  // Knight moves
            new Tuple<int, int>(1, 2), new Tuple<int, int>(1, -2), new Tuple<int, int>(-1, 2), new Tuple<int, int>(-1, -2)   // knight moves
        };

		for (int dir = 0; dir < directions.Length; dir++) {
			for (int tile = 1; tile < 8; tile++) {
				if (!Move.tuple_in_bounds(new Tuple<int, int>(origin.Item1 + directions[dir].Item1 * tile, origin.Item2 + directions[dir].Item2 * tile))) {continue;} // skip out of bounds
				Piece p = board[origin.Item1 + directions[dir].Item1 * tile, origin.Item2 + directions[dir].Item2 * tile];

				// valid enemy piece if its not null, opposite color, and not captured
				if (p != null && p.get_state() != State.Captured) {
					if (p.get_piece_color() != get_piece_color()) {
						switch (p.get_piece_type()) {
							case Piece.PieceType.Rook:
							    if (dir < 4) {
									threats.Add(p);
								} // ONLY add if its a horizontal or vertical
								break;
							case Piece.PieceType.Pawn:
								if (dir >= 4 && dir <= 7) {
									if (p.get_piece_color() == (int) Piece.PieceColor.White && directions[dir].Item1 == 1 && tile == 1) {
										// White pawns attack diagonally upward
										threats.Add(p);
									}
									else if (p.get_piece_color() == (int) Piece.PieceColor.Black && directions[dir].Item1 == -1 && tile == 1) {
										// Black pawns attack diagonally downward
										threats.Add(p);
									}
								}
								break;
							case Piece.PieceType.Bishop:
								if (dir >= 4 && dir <= 7) {
									threats.Add(p);
								}
								break;
							case Piece.PieceType.Queen:
								if (dir <= 7) {
									threats.Add(p);
								}
								
								break;
							case Piece.PieceType.Knight:
								if (dir >= 8) {
									if  (tile == 1) {
										threats.Add(p);
									}
								}
								break;
							case Piece.PieceType.King:
								if (tile == 1 && dir <= 7) {
									threats.Add(p);
								}
								break;
						}

						break; // ONLY remove if you plan to allow pieces to go through same color 
					}
					if (p.get_piece_color() == get_piece_color()) {
						break; // blocked threat, break here
					}

				}

			}

		}
	    

		return threats;
	} 


	/*
	Part of the initialization process of a Piece
	Using the default values set in the constructor, adds an image using the default sprite sheet
	*/
	public void SetIcon() {
		Texture2D Icons = ResourceLoader.Load<Texture2D>("res://Assets/ChessPieces.png");
		AtlasTexture Atlas = new AtlasTexture();
		TextureRect sprite = new TextureRect();

		Atlas.Atlas = Icons;
		sprite.Texture = Atlas;
		
		
		int row = 0;
		if (pcolor == PieceColor.Black) {
			row = 1;
		}
		int col = 0;
		
		switch (ptype)
		{
			case PieceType.Default:
				break;

			case PieceType.Pawn:
				col = 5;
				break;
			case PieceType.Rook:
				col = 4;
				break;
			case PieceType.Knight:
				col = 3;
				break;
				
			case PieceType.Bishop:
				col = 2;
				break;

			case PieceType.Queen:
				col = 1;
				break;

			case PieceType.King:
				col = 0;
				break;
		}
		Rect2 region = new Rect2(new Vector2(col * 2560/6, row * 2560/6), new Vector2(2560/6, 2560/6));
		sprite.Scale = new Vector2(0.0775f, 0.0775f);
		Atlas.Region = region;
		
		image = sprite;
		AddChild(sprite);

	}


	// handles the changing of piece states
	// will handle resets, and other things related to changing piece states
	// Argument: new_state = the state that you want to change to. 
	public void ChangeState(State new_state) {
		this.pstate = new_state;
		switch (this.pstate) 
		{
			case State.Placed:
				ResetState();
				break;
			case State.Captured:
				Visible = false;
				break;
		}
	}

	// private helper for ChangeState()
	// its only purpose is to cleanly reset the piece's state back to "placed"
	private void ResetState()
	{
		this.pstate = State.Placed;
		this.Scale = new Vector2(1, 1);
		this.Visible = true; // uncapture if captured
	}

	// IMPORTANT: This is updated manunally in "Player.cs" when a piece is moved
	// returns the State of the current piece
	public State get_state() {
		return this.pstate;
	}

	// returns the actual physics position vector of the current piece
	public Vector2 get_vector_position() {
		return GlobalPosition;
	}

	public void set_vector_position(Vector2 newPosition) { 
		this.GlobalPosition = newPosition;
		GlobalPosition = newPosition;
	}

	// returns the color of the piece
	// 1 for white
	// -1 for black
	public int get_piece_color() {
		return (int) pcolor;
	}	

	// returns the PieceType of the current piece
	public PieceType get_piece_type() {
		return ptype;
	}

	// methods below inteded for indexing the chessboard array only
	// ONLY FUNTIONS if board_position is manually updated. 
	public Tuple<int, int> get_board_position() {
		return PieceIndex;
	}

	// IMPORTANT: manual update for the board_position index.
	// Why is this necessary? Because I don't want to loop over to search for a piece index 
	public void set_board_position(Tuple<int, int> new_pos) {
		this.PieceIndex = new_pos;
	}

	/* Returns the string representation of a piece
	 NOTE: if you change this, please update the dependency for "prf_contains_exactly"
		inside of GameManager.cs
	*/
	public override String ToString() {
		return rep + "";
	}
}

// Represents the move history of a piece
// Records the last Move and capture made by a piece
public partial class PieceHistory {

	Piece[,] board;
	Piece p, c;
	Tuple<int, int> pold, cold;

	bool c_already_captured = false; // sometimes, unmaking a move causes a captured piece to be revealed


	public PieceHistory() {
		
	}
	public PieceHistory(Piece[,] board, Piece p, Piece c, Tuple<int, int> pold, Tuple<int, int> cold, bool c_cap) {
		this.board = board;
		this.p = p;
		this.c = c;
		this.pold = pold;
		this.cold = cold;
		this.c_already_captured = c_cap;
	}

	public Piece[,] get_board() {
		return board;
	}

	public Piece get_piece() {
		return p;
	}
	public Piece get_capture() {
		return c;
	}

	public bool already_captured() {
		return c_already_captured;
	}

	public Tuple<int, int> get_piece_index() {
		return pold;
	}

	public Tuple<int, int> get_cold_index() {
		return cold;
	}
}