using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;

public partial class GameManager : Node2D
{
	Board chess_board;

	PawnPromotion promotion;
	
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

	private Control game_info;

	// Stats
	int total_moves = 0; // moves = total_plies/ 2
	float time_secs = 0; // keep track of time 

	public Dictionary<int, List<Piece>> prf;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Board board = new Board(this);
		
		AddChild(board);
		board.gm = this;
		
		chess_board = board; // sets the board ref
		style_pieces((int) current_turn);
		prf = board.PieceRefs; // get the piece refs for easy access

		Player player= new Player(board);
		AddChild(player);

		game_info = GetNode<Control>("GameInfo");

		promotion = GetNode<PawnPromotion>("PawnPromotion");
		promotion.board = board;
		promotion.gm = this;
	}

	// Called when a move is made, after turns are swapped in alternate_turn() here.
	public void Update(bool incmoves = false) {
		total_moves += Convert.ToInt32(incmoves); // add moves

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
		
		// Stalemate, no legal moves, moves place king in check, or no possible moves
		if (get_current_turn() == Turn.White) {
			if (Board.White_King.get_threats(Board.White_King.get_board_position(), chess_board.BoardTiles).Count == 0
			&& all_threatened(Board.White_King) && !has_moves_stalemate(Piece.PieceColor.White)) {
				set_state(GameState.Stalemate);
			}
		} else if (get_current_turn() == Turn.Black) {
			if (Board.Black_King.get_threats(Board.Black_King.get_board_position(), chess_board.BoardTiles).Count == 0
			&& all_threatened(Board.Black_King) && !has_moves_stalemate(Piece.PieceColor.Black)) {
				set_state(GameState.Stalemate);
			}
		}

		
		
	}


	/*

	*/
	public void promote_pawn(Piece p) {
		promotion.StartPromotionWizard(p);
	}


	/* Returns whether the player of the given PieceColor has moves (function related to stalemate as defined in GameManager.cs)
		Assumes the given color is a valid index
	*/
	private bool has_moves_stalemate(Piece.PieceColor color) {
		foreach (Piece p in prf[(int) color]) {
			MoveManager mvm = new MoveManager(p, chess_board);
            Vector2 original_position = p.GlobalPosition;
            // dont consider pawn straight movement
            mvm.get_cardinal_movement(original_position); // set all cardinal
            mvm.get_knight_movement(original_position);
            mvm.get_intermediate_movement(original_position);	

			
			if (!p.is_pinned(p).Item1 && mvm.get_all_movement().Count > 0 ) {
				return true;
			}			
			else if (p.is_pinned(p).Item1) {
				Tuple<int, int> move = new Tuple<int, int>(p.is_pinned(p).Item2.get_board_position().Item2, 
															p.is_pinned(p).Item2.get_board_position().Item1);

				if (mvm.get_move_list_strings().Contains(move.ToString())) {
					return true; 
				}
			}
		}
		return false;
	}

	/* returns whether all the possible moves for the given input Piece p is under attack
        Used for the stalemate case in GameManager.cs
	*/
	private bool all_threatened(Piece p) {

        MoveManager mv = new MoveManager(p, chess_board); // just create a basic movemanager
         // get the king position we found
        mv.get_cardinal_movement(p.get_vector_position()); // set all cardinal
        mv.get_intermediate_movement(p.get_vector_position());     

        // if at any point the king can make a valid move, he is not checkmated
        foreach (Move m in mv.get_all_movement()) {
            // Note that since m.get_tuple() returns the x, y, you have to swap due since indexces are y, x
            Tuple<int, int> swap_idx = new Tuple<int, int>(m.get_tuple().Item2, m.get_tuple().Item1);
            if (p.get_threats(swap_idx, chess_board.BoardTiles).Count == 0) {
                return false;
            }
        }
		return true;
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

	public void set_info(String check_state, Tuple<int, int> last_move, Piece.PieceColor last_color, Piece last_piece) {
		game_info.GetNode<Label>("CheckState").Text = check_state + " on " + last_color.ToString();
		game_info.GetNode<Label>("MoveCounter").Text = "Moves: " + (total_moves / 2).ToString() + "\n" + "Turns: " + total_moves.ToString();
		game_info.GetNode<Label>("TurnActive").Text = get_current_turn().ToString() + " to move";
		game_info.GetNode<Label>("LastMove").Text = last_piece.ToString() + get_chess_rankfile(last_move);
	}

	/*
		Given a tuple with 2 ints row, column (NOT THE SAME FORMAT AS Piece.get_board_position())
	*/
	private string get_chess_rankfile(Tuple<int, int> move) {
		string rankfile = ((char) (97 + move.Item2)) + Math.Abs(8 - move.Item1).ToString();
		return rankfile;
	}


	// sets the state to the given GameManager.GameState
	public void set_state(GameState state) {
		this.state = state;
		GD.Print("State changed!"); 
	}

	public void set_moves(int moves) {
		total_moves = moves;
	}

	// returnns the current turn
    public Turn get_current_turn() {
		return current_turn;
	}

	public void set_current_turn(Turn turn) {
		current_turn = turn;
	}

	// swaps current turn to the other person
	// 	Updates game states Update()
	public void alternate_turn() {
		if (get_current_turn() == Turn.White) {current_turn = Turn.Black;}
		else if (get_current_turn() == Turn.Black) {current_turn = Turn.White;}
		Update(true); // updates the game state

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
