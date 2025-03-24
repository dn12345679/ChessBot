using Godot;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

public partial class Board : Node2D
{
    public Piece[,] BoardTiles;

    public GameManager gm;


    // MODDING: if you want to add a third player, change this
    public static Piece White_King; // Keep track of checks 
    public static Piece Black_King; // Keep track of checks

    const string DEFAULT_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


    public Label temp_label;

    public int CELL_SIZE = 32;
    public override void _Ready()
    {
        base._Ready();
        Label newLabel = new Label();
        AddChild(newLabel);
        newLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        
        newLabel.Position = new Vector2(300, 0);
        newLabel.Size = new Vector2(121, 121); 
        newLabel.Text = "dfasdf ";
        temp_label = newLabel;

    }

    // Constructor
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
    modify if you adding a new piece or osmething
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
                    Piece add_piece = new Piece(pvec, type, color, this, component);
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

        int file = (int)Math.Floor(pos.X / CELL_SIZE);
        int rank = (int)Math.Floor(pos.Y / CELL_SIZE);
        if (rank < 8 && rank > -1 && file < 8 && file > -1) {
            
            return new Tuple<int, int>(rank, file);
        }
        return null;
    
    }

    // DOES NOT HANDLE IF THE MOVE IS POSSIBLE. Seee Player.cs for full implementation
    // returns true if the move is valid
    // returns false if the move is invalid
    // IMPORTANT NOTE: the mti parameter is in the format (column, row) not (row, column)
    // p.is_pinned(p).Item2.get_board_position() IS ALSO in (column, row)

    /*
        Cases for validity:
            - Piece moving does not expose the king
            - Piece moving can expose the king, but moves in a way that doesn't
            - If King is in check, move MUST resolve the check by moving such that:
                - The vector between the new position and the attacking piece is parallel to
                    the vector between the king and the attacking piece, AND the magnitude of the vectur
                    between the new position and the attacking piece is SMALLER than the magnitude of the 
                    vector between the king position and the attacking piece
            - Or:
                - Check if the 16 possible directions contain an enemy piece that can capture AFTER making the move
                
    */
    public bool move_validation(Piece p, Tuple<int, int> mti) {
        // TODO:
        // validation:
            // move does not land on a same color piece
            // move does not open up weakness to King (same color)

        // if the move can capture or move in the same direction as the pinning piece, 
        // , then it can still be valid!!!
        if (p.is_pinned(p).Item1 == true) {
            // Item2 not null here by the nature that Item1 is true (check Piece.cs)
            Tuple<int, int> pin_tuple = p.is_pinned(p).Item2.get_board_position(); // Item2 is the pinning piece

            // Linear algebra time:
               // the vector between point "pinner" and "pinned" is elementwise vector subtraction
            Vector2 dir2pin = new Vector2(pin_tuple.Item1 - mti.Item1, pin_tuple.Item2 - mti.Item2); 

                // also want vector between king and the pinned piece
            Tuple<int, int> king = (p.get_piece_color() == (int) Piece.PieceColor.White) ? Board.White_King.get_board_position() : Board.Black_King.get_board_position();
            Vector2 dir2king = new Vector2(pin_tuple.Item1 - king.Item1, pin_tuple.Item2 - king.Item2);

                // 2 vectors are parallel if arc cosine of v1 dot v2 is equal to abs(v1 dot v2)
                // EASY: if their cross produoct v1 X v2 is 0

            // then return if their angle is 0 (cross prod). If so, then the move was valid
            return dir2pin.AngleTo(dir2king) == 0;
            }
        // if the piece is not pinned by a pinning piece
        if (p.is_pinned(p).Item1 == false) { 
            return true; 
            } // successful move
        return false; // false = invalid move, there is a pin or something
    }

    /* 
        TODO: finish this
        given an input Piece.PieceColor color, return whether the King
        associated with the color is under attack.
    */
    public bool is_checked(Piece.PieceColor color, Piece[,] board) {

        switch (color) {
            case Piece.PieceColor.Black:
                
                return Black_King.get_threats(Black_King.get_board_position(), board).Count > 0;
            case Piece.PieceColor.White:
                return White_King.get_threats(White_King.get_board_position(), board).Count > 0;
        }
        return false;
    }

    // Commits changes to making a move, assumes all validation was complete
    // Validation completed in move_validation, to be checked by myself 
    
    // Returns a PieceHistory object containing: 
        /*
            Piece[,] representing the OLD board unmodified
            Tuple<int, int> representing the OLD and NEW position (to get the vector just multiply by 32)
            Piece representing the OLD AND NEW CAPTURED PIECE (or null)
        */
    public PieceHistory make_move(Piece p, Tuple<int, int> mti) {
        if (!move_validation(p, mti)) { return new PieceHistory();} // if its not valid, then just return
        Vector2 newPosition = new Vector2(mti.Item2 * CELL_SIZE, mti.Item1 * CELL_SIZE); // new position to move to
        
        Tuple<int, int> oldBoardPosition = p.get_board_position(); // just multiply by CELL_SIZE for vector
        Piece[,] old_board = BoardTiles.Clone() as Piece[,]; // return the old Board
        Piece captured_piece = BoardTiles[mti.Item1, mti.Item2]; // null, or a piece
        

        Tuple<int, int> old_mti = p.get_board_position(); // get this piece's previous position

        // Delete captured piece if it exists
        if (BoardTiles[mti.Item1, mti.Item2] != null) {
            captured_piece.ChangeState(Piece.State.Captured); // kill it    
        }
        
        // set the new piece on that position
        p.set_board_position(mti); // updates the move tuple indices
        p.set_vector_position(newPosition); // updates the position reference

        // set the n ew position to this piece on the board representation
        // null the old positions
        BoardTiles[mti.Item1, mti.Item2] = p;
        BoardTiles[old_mti.Item1, old_mti.Item2] = captured_piece;
        
        //GD.Print(BoardTiles[mti.Item1, mti.Item2]);  should be the piece you just moved
        //GD.Print(BoardTiles[old_mti.Item1, old_mti.Item2]); should be the old piece

        return new PieceHistory(old_board, p, captured_piece, oldBoardPosition, mti); // returns the old board
    }

    // Given an input PieceHistory object (see implementation in Piece.cs
    // unmakes the previous move recorded in phist
    // ONLY INTENDED FOR BOT and other cool things, no undo move in real game

    public void unmake_move(PieceHistory phist) {
        Piece[,] history_board = phist.get_board();
        Piece pold = phist.get_piece();
        Piece cold = phist.get_capture();
        Tuple<int, int> pidx = phist.get_piece_index();
        Tuple<int, int> cidx = phist.get_cold_index();

        if (cold != null) {
            cold.ChangeState(Piece.State.Placed); // add the piece back
        }
        
        pold.set_board_position(pidx);
        Vector2 vectorposition = new Vector2(pidx.Item2 * CELL_SIZE, pidx.Item1 * CELL_SIZE);
        pold.set_vector_position(vectorposition);

        BoardTiles = history_board;

    }
    

    // Returns the string representation of the Chess Board
    // Piece.cs contains the String representation for each piece
    public override string ToString()
    {

        string return_string = "[";
        for (int y = 0; y < BoardTiles.GetLength(0); y++)
        {
            string row = "";
            for (int x = 0; x < BoardTiles.GetLength(1); x++)
            {
                row += BoardTiles[y, x]?.ToString() ?? ".";
                row += " ";
            }
            return_string += row; // Print each row
        }


        return return_string;
    }

    /*
        Class representing the Board Tiles. 
        Does not contain anything relevant to the Chess pieces, only for visual decoration
    */    
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
