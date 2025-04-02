using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


public partial class Board : Node2D
{

    public Piece[,] BoardTiles;

    public Dictionary<int, List<Piece>> PieceRefs = new Dictionary<int, List<Piece>>{} ; // quick ref for pieces
    // pieces are to be removed from here if they are captured, and added back if "unmake_move()" is called by Player.cs

    public GameManager gm;


    // MODDING: if you want to add a third player, change this
    public static Piece White_King; // Keep track of checks 
    public static Piece Black_King; // Keep track of checks

    const string DEFAULT_FEN = "rnbqkbn1/pppppp1P/8/4q3/1n6/6q1/PPPPnPPP/RNBQKBNR w KQkq - 0 1"; // DO NOT CHANGE

    public string fen = DEFAULT_FEN; // feel free to change this as long as it fits format

    public int CELL_SIZE = 32;
    public int DIMENSION_X = 8;
    public int DIMENSION_Y = 8;

    public Tuple<int, int> en_passant_square = null;


    // Constructor
    public Board(GameManager gm) {
        this.gm = gm;
        BoardTiles = new Piece[DIMENSION_Y,DIMENSION_X];
        CreateBoard(); // tiles
        ReadForsythEdwards(fen); // pieces
        
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


    // Returns the rank, file in the form CHARINT (ex: e4)
    private string get_chess_rankfile(Tuple<int, int> move) {
		string rankfile = ((char) (97 + move.Item2)) + Math.Abs(8 - move.Item1).ToString();
		return rankfile;
	}

    /*
        Given a string input in the format charint, returns a tuple representing the ARRAY INDEX
            of that tile.
        Example: 
           - the input "g3" should map to the colloquial tile (7, 3) from white perspective, however
            the array indices of the tile at "g3" is actually (5, 6) when accounting for array technicalities 
    */
    private Tuple<int, int> get_chess_tuple(string rankfile) {
        if (rankfile == null || rankfile.Length == 1) {
            return null; 
        }
        char[] components = rankfile.ToCharArray();
        Tuple<int, int> tuple = new Tuple<int, int>('8' - components[1], components[0] - 97);
        return tuple;
    }

    /*
    Assumes standard FEN notation, separated by a / symbol
    modify if you adding a new piece or osmething

    adds pieces to PieceRefs dict
    sets King references

    default "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    */
    private void ReadForsythEdwards(String FEN) {

        String PiecePositions = FEN.Split(" ")[0]; // the piece board itself
        String CurrentMove = FEN.Split(" ")[1]; // White or Black to move
        char[] CastlingAvailability = FEN.Split(" ")[2].ToCharArray(); // KQ or kq for black and white 
        String EnPassantSquare = FEN.Split(" ")[3];
        int HalfMove = Int32.Parse(FEN.Split(" ")[4]); // 50 move draw (UNUSED)
        int FullMove = Int32.Parse(FEN.Split(" ")[5]); // total moves

        // set the move and turns
        gm.set_moves(FullMove * 2); // gm actually keeps track of total "moves" (as in each turn)
        
        // set the current turn
        if (CurrentMove.Equals("b")) {gm.set_current_turn(GameManager.Turn.Black);}
        else if (CurrentMove.Equals("w")) {gm.set_current_turn(GameManager.Turn.White);}

        en_passant_square = get_chess_tuple(EnPassantSquare); // gets the en passant square as array indices


        // iterating variables
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

                    // Castling availability
                    if (char.ToLower(component).Equals('r') && ((color == -1 && col == 0 && !CastlingAvailability.Contains('q')) || 
                    (color == -1 && col == DIMENSION_X - 1 && !CastlingAvailability.Contains('k')) ||
                    (color == 1 && col == 0 && !CastlingAvailability.Contains('Q')) || 
                    (color == 1 && col == DIMENSION_X - 1 && !CastlingAvailability.Contains('K')))) {
                        add_piece.ChangeState(Piece.State.Placed);
                    }



                    // I FORGOT WHY color is swapped. but just roll with it
                    // -color to swap the color because Piece.cs says so and I don't want to figure out why
                    // to keep track of each piece by it's color
                    // simplifies piece obtaining.
                    if (PieceRefs.ContainsKey(-color)) {
                        PieceRefs[-color].Add(add_piece);
                    }
                    else{
                        List<Piece> pieces = new List<Piece>{add_piece};
                        PieceRefs.Add(-color, pieces);
                    }


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
    public bool move_validation(Piece p, Tuple<int, int> mti, Tuple<bool, List<Piece>> check_info) {
    /* 
        Optimization:
            - Check if piece is pinned:
                - If king is not checked, moves are only valid if its in the direction of the attacker
                - if king is checked, DON"T get any moves for this piece. 
            - Check if piece is not pinned:
                - if king is not checked, CONTINUE, free to move
                - if king is checked, moves are only valid if it intersects with the vector between the king and attacker
    */
        // If at any point the move is in between any of the 2 attackers, the move is invalid 
        if (p.get_piece_type() == Piece.PieceType.King) {
            if (check_info.Item1 == true) {
                foreach (Piece attacker in check_info.Item2) {

                    if (is_between_king_attacker(p, mti, attacker.get_board_position())
                    && !tuples_equal(mti, attacker.get_board_position())) {
                        return false;
                    }

                }
                return true; // otherwise the move is valid if none of the attackers cover the square
            }
        }

        // piece is not a king at this point. If the king has more than 1 attacker, blocking is not an option
        if (check_info.Item2.Count > 1) {
            return false;
        }
        

        // if the move can capture or move in the same direction as the pinning piece, 
        // , then it can still be valid!!!
        if (p.is_pinned(p).Item1 == true) {
            if (check_info.Item1 == true) {return false;}
            
            // if the piece is pinned, and the king is not checked, make sure the move stays within king/attacker
            return is_between_king_attacker(p, mti, p.is_pinned(p).Item2.get_board_position());
            }
        // if the piece is not pinned by a pinning piece
        if (p.is_pinned(p).Item1 == false) { 
            
            if (check_info.Item1 == true) {
            // Item2 not null here by the nature that Item1 is true (check Piece.cs)
                GD.Print(is_between_king_attacker(p, mti, check_info.Item2[0].get_board_position()) + " " + p);
                return is_between_king_attacker(p, mti, check_info.Item2[0].get_board_position());
            }

            return true; // valid move, no check no pink

            } // successful move
        return false; // false = invalid move, there is a pin or something
    }

    private bool is_between_king_attacker(Piece p, Tuple<int, int> mti, Tuple<int, int> apos) {
        // Item2 not null here by the nature that Item1 is true (check Piece.cs)
        Tuple<int, int> pin_tuple = apos; // Item2 is the pinning piece

        // Linear algebra time:
            // the vector between point "pinner" and "pinned" is elementwise vector subtraction
        Vector2 dir2pin = new Vector2(pin_tuple.Item1 - mti.Item1, pin_tuple.Item2 - mti.Item2); 

            // also want vector between king and the pinned piece
        Tuple<int, int> king = (p.get_piece_color() == (int) Piece.PieceColor.White) ? Board.White_King.get_board_position() : Board.Black_King.get_board_position();
        Vector2 dir2king = new Vector2(pin_tuple.Item1 - king.Item1, pin_tuple.Item2 - king.Item2);

            // 2 vectors are parallel if arc cosine of v1 dot v2 is equal to abs(v1 dot v2)
            // EASY: if their cross produoct v1 X v2 is 0

        // then return if their angle is 0 (cross prod). If so, then the move was valid
        // Checking this manner may often result in issues if the mti and apos are the same,
            // , so in that case check if the mti and apos are equal, then capture is possible
        return dir2pin.AngleTo(dir2king) == 0 || tuples_equal(mti, apos);
    }

    /* 
        given an input Piece.PieceColor color, return whether the King
        associated with the color is under attack.
    */
    public Tuple<bool, List<Piece>> is_checked(Piece.PieceColor color, Piece[,] board, Tuple<int, int> pos = null) {

        switch (color) {
            case Piece.PieceColor.Black:
                if (pos == null) {pos = Black_King.get_board_position();}
                return new Tuple<bool, List<Piece>>(Black_King.get_threats(pos, board).Count > 0, Black_King.get_threats(pos, board));
            case Piece.PieceColor.White:
                if (pos == null) {pos = White_King.get_board_position();}
                return new Tuple<bool, List<Piece>>(White_King.get_threats(pos, board).Count > 0, White_King.get_threats(pos, board));
        }
        return new Tuple<bool, List<Piece>>(false, null);
    }


    /*
        To be called by the "attacker" piece on the Opposite color
        Returns whether the king of the color is checkmated
            Conditions:
                - King has no valid moves
                - "Unpinned" pieces cannot block checkmates
                    - To optimize, I calculate the orientation to skip impossible pieces
                - King is directly in check by "attacker"

        Inputs:
            - color: the Piece.PieceColor OPPOSITE of the color of the Piece attacker
            - board: the board to check for intersection
            - attacker: the piece attacking the king of color "color"
                - Naturally, there should only be 1 piece "attacking" the king at the same time
        How does it work?
            - assumes the game is over; the king has been checkmated
            - loops over all possible conditions that would render the king not checkmated
            - Not checkmated if at any point there is a valid move
    */
    public bool is_checkmated(Piece.PieceColor color, Piece[,] board, Piece attacker) {

        Vector2 apos = attacker.get_vector_position(); // attacker position
        Vector2 kpos = new Vector2(); // king position 

        Tuple<int, int>[] directions = new Tuple<int, int>[16] {
            new Tuple<int, int>(0, -1), new Tuple<int, int>(1, 0), new Tuple<int, int>(0, 1), new Tuple<int, int>(-1, 0),   // Horizontal/Vertical
            new Tuple<int, int>(-1, -1), new Tuple<int, int>(1, 1), new Tuple<int, int>(-1, 1), new Tuple<int, int>(1, -1),   // Diagonal
            new Tuple<int, int>(2, 1), new Tuple<int, int>(2, -1), new Tuple<int, int>(-2, 1), new Tuple<int, int>(-2, -1),  // Knight moves
            new Tuple<int, int>(1, 2), new Tuple<int, int>(1, -2), new Tuple<int, int>(-1, 2), new Tuple<int, int>(-1, -2)   // knight moves
        };

        List<int> idx = new List<int>();

        switch (color) {
            case Piece.PieceColor.Black:
                kpos = Black_King.get_vector_position();
                break; 
            case Piece.PieceColor.White:
                kpos = White_King.get_vector_position();
                break;
        }   

        Vector2 posp; // to check intersections for each individual piece

        List<Piece> possible = new List<Piece>(); // possible blockers

        // optimization
        // step 1: add hte indexes of the directions as written above. Each "direction" is in form (y, x) or (row, column)
        foreach (Piece p in PieceRefs[(int) color]) {

            posp = p.get_vector_position(); // get the base
            idx = new List<int>();
            switch (p.get_piece_type()) {
                case Piece.PieceType.Rook:
                    idx.AddRange(Enumerable.Range(0, 4));

                    break;
                case Piece.PieceType.Bishop:
                    idx.AddRange(Enumerable.Range(4, 8));
                    break;
                case Piece.PieceType.Queen:
                    idx.AddRange(Enumerable.Range(4, 8));
                    break;
                case Piece.PieceType.Pawn:
                    if (p.get_piece_color() == (int) Piece.PieceColor.Black) {
                        // White pawns attack diagonally upward
                        idx.Add(1);
                        idx.Add(7);
                        idx.Add(5);
                    }
                    else if (p.get_piece_color() == (int) Piece.PieceColor.White) {
                        // Black pawns attack diagonally downward
                        idx.Add(3);
                        idx.Add(6);
                        idx.Add(4);
                    }
                    break;
                case Piece.PieceType.Knight:
                    idx.AddRange(Enumerable.Range(9, 16));
                    break;
            }
            // step 2: iterate over possible vectors and search for possible intersections

            switch (p.get_piece_type()) {
                case Piece.PieceType.Rook:
                case Piece.PieceType.Bishop:
                case Piece.PieceType.Queen:
                    foreach (int id in idx) {
                        if (intersectable(kpos, attacker.get_vector_position(), 
                            posp, posp + new Vector2(directions[id].Item1, directions[id].Item2) * 8 * CELL_SIZE)) {
                                possible.Add(p);
                            }
                    }
                    break;
                // Pawns have a unique case since if they intersect, they are guaranteed to be blockable
                case Piece.PieceType.Pawn:
                    for(int i = 0; i < idx.Count; i++) {
                        int id = idx[i]; 
                        // captures only
                        if ( i > 0) {
                            Tuple<int, int> intersect = new Tuple<int,int>((int) posp.Y/32 + directions[id].Item1, 
                                                                            (int)posp.X/ 32 + directions[id].Item2);

                            if (Move.tuple_in_bounds(intersect) && BoardTiles[intersect.Item1, intersect.Item2] == attacker
                            && !p.is_pinned(p).Item1) {
                                return false; // unpinned and able to capture
                            }
                        }
                        // moves only
                        else{
                            if (intersectable(kpos, attacker.get_vector_position(), 
                            posp, posp + new Vector2(directions[id].Item1, directions[id].Item2) * 1 * CELL_SIZE) || intersectable(kpos, attacker.get_vector_position(), 
                            posp, posp + new Vector2(directions[id].Item1, directions[id].Item2) * 2 * CELL_SIZE))

                            possible.Add(p);
                        }

                    }
                        
                    break;
                case Piece.PieceType.Knight:
                    possible.Add(p); // i really don't know how ot handle this one
                    break;
            }
            
        }
        


        // step 3: get all the tiles that need to be covered (exclusive of the king)
        Vector2 dir = (kpos - apos) / (kpos-apos).Length(); // direction from attacker to king
        Vector2 curr_pos = attacker.get_vector_position(); // to iterate
        List<Tuple<int, int>> to_cover = new List<Tuple<int, int>>{}; // need to cover these tiles
        dir = new Vector2((int)Math.Round(dir.X), (int)Math.Round(dir.Y)); // normalize to a direction

        while (curr_pos != kpos && Move.tuple_in_bounds(new Tuple<int, int>((int) curr_pos.Y / CELL_SIZE, (int) curr_pos.X / CELL_SIZE))) {
            //break; // idk what broke here
             
            to_cover.Add(new Tuple<int, int>((int) curr_pos.Y / CELL_SIZE, (int) curr_pos.X / CELL_SIZE));
            curr_pos += new Vector2(dir.X, dir.Y) * CELL_SIZE;
        }
        // step 4: check if any of the "possible" pieces

        // NOTE FUTURE ME: optimize this by using  the fact that "check" only happens from 1 direction
        foreach (Piece p in possible) {
           
            MoveManager mvm = new MoveManager(p, this);
            Vector2 original_position = p.GlobalPosition;
            // dont consider pawn straight movement
            mvm.get_cardinal_movement(original_position); // set all cardinal
            mvm.get_knight_movement(original_position);
            mvm.get_intermediate_movement(original_position);
            // SEPERATE CONDITION FOR PAWNS 
            if (p.get_piece_type() != Piece.PieceType.Pawn) {
                if (mvm.get_move_list_strings().Any(to_cover.Select(t => $"({t.Item1}, {t.Item2})").ToList().Contains)) {
                    
                    return false; // SOMETHING CAN BLOCK THE KING
                }
            }
            else {
                // pawn logic
                foreach (Move m in mvm.get_all_movement()) {
                    // Woah this is weird; its because the tuple format for "Move"
                        // objects are different from the tuple format for "get_board_position()"
                    if (m.get_tuple().Item1 == p.get_board_position().Item2 
                    && m.get_tuple().Item1 == attacker.get_board_position().Item2) {
                        continue; // cant capture by going straight
                    }
                    // ONLY CAPTURES OR VALID BLOCKS 
                    else if (mvm.get_move_list_strings().Any(to_cover.Select(t => $"({t.Item1}, {t.Item2})").ToList().Contains)) {
                        return false; // SOMETHING CAN BLOCK THE KING
                    }
                }
            }

        }

        // step 5: IF THE ABOVE CHUNK didnt return,
            // that implies that all pieces that can make the block are pinned by other pieces. 
        
        
        Piece king = (color == Piece.PieceColor.White) ? Board.White_King : Board.Black_King;
        MoveManager mv = new MoveManager(king, this); // just create a basic movemanager
         // get the king position we found
        mv.get_cardinal_movement(kpos); // set all cardinal
        mv.get_intermediate_movement(kpos);     
        
        // if at any point the king can make a valid move, he is not checkmated
        foreach (Move m in mv.get_all_movement()) {
            
            // Note that since m.get_tuple() returns the x, y, you have to swap due since indexces are y, x
            Tuple<int, int> swap_idx = new Tuple<int, int>(m.get_tuple().Item2, m.get_tuple().Item1);
            if (king.get_threats(swap_idx, board).Count == 0) {
                
                return false;
            }
        }
        //CHECKMATE
        return true; // the game is over
    }

    // returns whether there is a valid intersection before checking a piece's moves
    private bool intersectable(Vector2 King, Vector2 Enemy, Vector2 Target, Vector2 Border) 
    {
        // see https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
        float o1 = orientation(King, Enemy, Target);
        float o2 = orientation(King, Enemy, Border);
        float o3 = orientation(Target, Border, King);
        float o4 = orientation(Target, Border, Enemy);

        // intersection found by orientation
        if (o1 != o2 && o3 != o4) {
            return true;
        }

        // collinearity
        if (o1 == 0 && on_segment(King, Enemy, Target)) {return true;}
        if (o2 == 0 && on_segment(King, Enemy, Border)) {return true;}
        if (o3 == 0 && on_segment(Target, Border, King)) {return true;}
        if (o4 == 0 && on_segment(Target, Border, Enemy)) {return true;}
        return false; // no intersection, don't check this piece
    }

    /* returns the orientation of 3 points in space, where AB is a segment,
        and C is a point
        Helper method for is_checkmated
    */
    private float orientation(Vector2 A, Vector2 B, Vector2 C) {
        float prod = (B.X - A.X) * (C.Y - A.Y) - (B.Y - A.Y) * (C.X - A.X);
        if (prod > 0) {
            return 1;
        }
        else if (prod < 0) {
            return -1;
        }
        else{
            return 0;
        }
    }

    /*
    Returns whether point C lies on segment AB in the exception that 
        the orientation is 0.
    */
    private bool on_segment(Vector2 A, Vector2 B, Vector2 C) {
        return Math.Min(A.X, B.X) <= C.X && C.X <= Math.Max(A.X, B.X) 
            && Math.Min(A.Y, B.Y) <= C.Y && C.Y <= Math.Max(A.Y, B.Y);
    }

    // Optimization: faster to compare across values of a tuple than string conversion
    private bool tuples_equal(Tuple<int, int> A, Tuple<int, int> B) {
        return A.Item1 == B.Item1 && A.Item2 == B.Item2;
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
        
        Vector2 newPosition = new Vector2(mti.Item2 * CELL_SIZE, mti.Item1 * CELL_SIZE); // new position to move to
        
        Tuple<int, int> oldBoardPosition = p.get_board_position(); // just multiply by CELL_SIZE for vector
        Piece[,] old_board = BoardTiles.Clone() as Piece[,]; // return the old Board
        Piece captured_piece = BoardTiles[mti.Item1, mti.Item2]; // null, or a piece
        
        bool c_already_captured = false; // for scenarios where "unmakes" reveal a piece

        Tuple<int, int> old_mti = p.get_board_position(); // get this piece's previous position

        Piece.State pso = p.get_state(); // save old state
        Piece.State cso = Piece.State.Unmoved;

        
        // Delete captured piece if it exists, and remove its reference from PieceRefs
        if (BoardTiles[mti.Item1, mti.Item2] != null) {
            cso = BoardTiles[mti.Item1, mti.Item2].get_state(); 
            if (BoardTiles[mti.Item1, mti.Item2].get_state() == Piece.State.Captured) {c_already_captured = true;}
            // prevent forward capturing  by pawns
            if (!(p.get_piece_type() == Piece.PieceType.Pawn && p.get_board_position().Item2 == mti.Item2)) {
                captured_piece.ChangeState(Piece.State.Captured); // kill it    
                PieceRefs[captured_piece.get_piece_color()].Remove(captured_piece); // remove its reference             
            }
        }
        
        // set the new piece on that position
        p.set_board_position(mti); // updates the move tuple indices
        p.set_vector_position(newPosition); // updates the position reference

        // set the n ew position to this piece on the board representation
        // null the old positions
        BoardTiles[mti.Item1, mti.Item2] = p;
        BoardTiles[old_mti.Item1, old_mti.Item2] = captured_piece;
        
        //(BoardTiles[mti.Item1, mti.Item2]);  should be the piece you just moved
        //(BoardTiles[old_mti.Item1, old_mti.Item2]); should be the old piece

        return new PieceHistory(old_board, p, captured_piece, oldBoardPosition, mti, c_already_captured, pso, cso); // returns the old board
    }

    // Given an input PieceHistory object (see implementation in Piece.cs
    // unmakes the previous move recorded in phist
    // Used when move puts king in check illegally

    public void unmake_move(PieceHistory phist) {
        Piece[,] history_board = phist.get_board();
        Piece pold = phist.get_piece();
        Piece cold = phist.get_capture();
        Tuple<int, int> pidx = phist.get_piece_index();
        Tuple<int, int> cidx = phist.get_cold_index();

        Piece.State pso = phist.get_pstate();
        Piece.State cso = phist.get_cstate();
        bool already_captured = phist.already_captured(); // dont reveal if piece already captured

        if (cold != null && !already_captured) {
            cold.ChangeState(cso); // add the piece back
            PieceRefs[cold.get_piece_color()].Add(cold); // add it back to pieceRefs too
        }
        
        pold.set_board_position(pidx);
        Vector2 vectorposition = new Vector2(pidx.Item2 * CELL_SIZE, pidx.Item1 * CELL_SIZE);
        pold.set_vector_position(vectorposition);
        
        pold.ChangeState(pso); // reset states

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
