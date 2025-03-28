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
		pold = null;

		// OTHER LOGIC: CHECK if king is checked, etc

	}
}
