using Godot;
using System;

public partial class PawnPromotion : Control
{	
	public GameManager gm;
	public Board board;

	public Piece pold;

    // Ref
	OptionButton selector;
	Button confirm; 

    public override void _Ready()
    {
        base._Ready();
		selector = GetNode<OptionButton>("Selection");
		confirm = GetNode<Button>("Confirm");
		
		confirm.Connect("pressed", new Callable(this, MethodName.on_confirm_pressed));

		
    }
	
	
	public PawnPromotion() {
		
	}

	public void StartPromotionWizard(Piece pold) {
		Visible = true;
		GetTree().Paused = true;
		this.pold = pold; // start with this piece
	}

	private Tuple<int, char> get_option(string option) {
		switch (option) {
			case "Queen":
				return new Tuple<int, char>(5, 'q');
			case "Rook":
			    return new Tuple<int, char>(2, 'r');
			case "Bishop":
				return new Tuple<int, char>(4, 'b');
			case "Knight":
				return new Tuple<int, char>(3, 'n');
		}
		return null;

	}

	public void on_confirm_pressed() {
		// first remove the old piece reference
		if (board.PieceRefs.ContainsKey(pold.get_piece_color())) {
			board.PieceRefs[pold.get_piece_color()].Remove(pold);
		}

		// initialize a new piece
		int row = pold.get_board_position().Item1;
		int col = pold.get_board_position().Item2;
		Tuple<int, char> option = get_option(selector.GetItemText(selector.GetSelectedId()));

		Vector2 pvec = new Vector2(col * 32, row * 32);
		Piece add_piece = new Piece(pvec, option.Item1, -pold.get_piece_color(), board, option.Item2);
                    
		
		board.AddChild(add_piece);
		add_piece.set_board_position(new Tuple<int, int>(row, col)); // sets the index

		// delete the old piece
		pold.QueueFree();
        board.PieceRefs[pold.get_piece_color()].Add(add_piece);

		board.BoardTiles[row, col] = add_piece;

		GetTree().Paused = false; // unpause
		Visible = false;

		add_piece.Modulate = new Color(1f, 1f, 1f, 0.3f); // recolor it as the "turn" is over

		// CHECKING LOGIC (copied from Player.cs)

		// get the king of opposite color, you are the attacker
		// also get the color of the possible blockers
		Piece.PieceColor color = (pold.get_piece_color() == (int) Piece.PieceColor.White) ? Piece.PieceColor.Black : Piece.PieceColor.White;
		Piece king = (pold.get_piece_color() == (int) Piece.PieceColor.White) ? Board.Black_King : Board.White_King;
		
		// handle check and checkmate
		string state = "";
		// state set here. Perspective of hte attacker
		if (board.is_checked(color, board.BoardTiles)) {
			state = "Check";
			if (board.is_checkmated(color, board.BoardTiles, king.get_threats(king.get_board_position(), board.BoardTiles)[0])) {
				GameManager.GameState gs = (pold.get_piece_color() == (int)Piece.PieceColor.White) ? GameManager.GameState.White_win : GameManager.GameState.Black_win;
				board.gm.set_state(gs);
				state = "Checkmate";
			}
		}

		board.gm.set_info(state, new Tuple<int, int>(row, col), (Piece.PieceColor) (-pold.get_piece_color()), add_piece);
		pold.QueueFree();
	}
}
