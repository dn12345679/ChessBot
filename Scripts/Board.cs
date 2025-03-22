using Godot;
using System;
using System.Diagnostics;

public partial class Board : Node2D
{
    public Piece[,] BoardTiles;

    public GameManager gm;


    // MODDING: if you want to add a third player, change this
    public static Piece White_King; // Keep track of checks 
    public static Piece Black_King; // Keep track of checks

    const string DEFAULT_FEN = "rnbqkbnr/pppppppp/8/8/8/8/QPPPPPPP/RNBQKBNR w KQkq - 0 1";


    public int CELL_SIZE = 32;
    public Board() {
        BoardTiles = new Piece[8,8];
        CreateBoard(); // tiles
        ReadForsythEdwards(DEFAULT_FEN); // pieces
    }

    // Generates a board of 64 tiles for the chess board
	public void CreateBoard() {
        for (int row = 0; row < 8; row++){
            for (int col = 0; col < 8; col++) {
                Color tColor = new Color(0.7f, 0.54f, 0.41f);
                if (((row + col) % 2) == 0) {
                    tColor = new Color(0.34f, 0.22f, 0.11f);
                }
                Vector2 tPosition = new Vector2(row * 32, col * 32);

                BoardTile newTile = new BoardTile(tPosition, tColor);
                
                AddChild(newTile);
            }
        }
    }

    /*
    Assumes standard FEN notation, separated by a / symbol
    */
    private void ReadForsythEdwards(String FEN) {

        String PiecePositions = FEN.Split(" ")[0]; // the piece board itself
        String CurrentMove = FEN.Split(" ")[1]; // White or Black to move
        String CastlingAvailability = FEN.Split(" ")[2]; // KQ or kq for black and white 
        //String EnPassantSquare = FEN.Split(" ")[3];
        int HalfMove = Int32.Parse(FEN.Split(" ")[4]); // 50 move draw
        int FullMove = Int32.Parse(FEN.Split(" ")[5]); // total moves

        int col = 0;
        int row = 0;


        int color = 0;
        int type = 0;

        foreach (char component in PiecePositions) {
            if (component == '/') {
                col = 0; 
                row += 1;
            }
            else{
                if (char.IsDigit(component) ) {
                    col += (int) char.GetNumericValue(component);

                } else {
                    if (char.IsLower(component)) {
                        color = -1;
                    }
                    else if (char.IsUpper(component)) {
                        color = 1;
                    }

                    char ptype = char.ToLower(component);
                    switch (ptype) {
                        case 'p':
                            type = 1;
                            break;
                        case 'r':
                            type = 2;
                            break;
                        case 'n':
                            type = 3;
                            break;
                        case 'b':
                            type = 4;
                            break;
                        case 'q':
                            type = 5;
                            break;
                        case 'k':
                            type = 6;
                            break;
                    }

                    
                    Vector2 pvec = new Vector2(col * 32, row * 32);
                    Piece add_piece = new Piece(pvec, type, color, this, ptype);
                    BoardTiles[row, col] = add_piece;
                    AddChild(add_piece);
                    add_piece.set_board_position(new Tuple<int, int>(row, col)); // sets the index

                    // special case: dont change unless for modding, sets the king references
                    if (color == 1 && type == 6) {
                        Board.White_King = BoardTiles[row, col];
                    }
                    else if (color == -1 && type == 6) {
                        Board.Black_King = BoardTiles[row, col];
                    }

                    col++;


                }
            }
        }
    }

    // Returns the index of the mouse position on the board if it exists
    // null otherwise
    public Tuple<int, int> GetIndexUnderMouse() {

        Vector2 pos = GetGlobalMousePosition();

        int file = (int)Math.Floor(pos.X / 32);
        int rank = (int)Math.Floor(pos.Y / 32);
        if (rank < 8 && rank > -1 && file < 8 && file > -1) {
            
            return new Tuple<int, int>(rank, file);
        }
        return null;
    
    }

    // returns true if the move is valid
    // returns false if the move is invalid
    // IMPORTANT NOTE: the mti parameter is in the format (column, row) not (row, column)
    // p.is_pinned(p).Item2.get_board_position() IS ALSO in (column, row)
    public bool move_validation(Piece p, Tuple<int, int> mti) {
        // TODO:
        // validation:
            // move does not land on a same color piece
            // move does not open up weakness to King (same color)

        // if the move can capture the pinning piece
        if (p.is_pinned(p).Item1 == true) {
            Tuple<int, int> pin_tuple = p.is_pinned(p).Item2.get_board_position();
            return pin_tuple.Item2 == mti.Item2 && pin_tuple.Item1 == mti.Item1;
            }
        // if the piece is not pinned by a pinning piece
        if (p.is_pinned(p).Item1 == false) { return true; } // successful move
        
        return false; // false = invalid move, there is a pin or something
    }

    // Commits changes to making a move, assumes all validation was complete
    // Validation completed in move_validation, to be checked by myself 
    public Piece[,] make_move(Piece p, Tuple<int, int> mti) {
        Vector2 newPosition = new Vector2(mti.Item2 * 32, mti.Item1 * 32); // new position to move to
        Piece[,] old_tiles = BoardTiles; // return the old Board, UNUSED
        Tuple<int, int> old_mti = p.get_board_position(); // get this piece's previous position

        // Delete captured piece if it exists
        if (BoardTiles[mti.Item1, mti.Item2] != null) {
            BoardTiles[mti.Item1, mti.Item2].QueueFree(); // kill it
            BoardTiles[mti.Item1, mti.Item2] = null;        
        }
        
        // set the new piece on that position
        p.set_board_position(mti); // updates the move tuple indices
        p.set_vector_position(newPosition); // updates the position reference

        // set the n ew position to this piece on the board representation
        // null the old positions
        BoardTiles[mti.Item1, mti.Item2] = p;
        BoardTiles[old_mti.Item1, old_mti.Item2] = null;
        
        //GD.Print(BoardTiles[mti.Item1, mti.Item2]);  should be the piece you just moved
        //GD.Print(BoardTiles[old_mti.Item1, old_mti.Item2]); should be the old piece

        return BoardTiles; // returns the old board
    }
    
    
   private partial class BoardTile : Node2D
   {
        private Vector2 tPosition;
        private Color tColor;
        private Rect2 tRect;
        public BoardTile(Vector2 tPosition, Color tColor) {
            this.tPosition = tPosition;
            this.tColor = tColor;
            Vector2 size = new Vector2(32, 32);
            tRect = new Rect2(tPosition, size);
            QueueRedraw();
        }
        public override void _Draw()
        {
            DrawRect(tRect, tColor);
        }
   }
}
