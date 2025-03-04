using Godot;
using System;
using System.Diagnostics;

public partial class Board : Node2D
{
    public static Piece[] BoardTiles;

    const string DEFAULT_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public Board() {
        BoardTiles = new Piece[64];
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

                    col++;
                    Vector2 pvec = new Vector2(col * 32 - 32, row * 32);
                    Piece add_piece = new Piece(pvec, type, color);
                    BoardTiles[row * 7 + col] = add_piece;
                    AddChild(add_piece);
                }
            }
        }
    }


    private void SetPieces() {
        
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
