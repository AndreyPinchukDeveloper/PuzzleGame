using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle : MonoBehaviour
{
    public ArrayLayout boardLayout;

    [Header("UI Elements")]
    public Sprite[] pieces;
    public RectTransform gameBoard;
    public RectTransform fallsBoard;

    [Header("Prefabs")]
    public GameObject nodePiece;
    public GameObject fallsPiece;

    int score = 0;
    int width = 9;
    int height = 14;
    int[] fills;
    Node[,] board;

    List<NodePiece> update;
    List<FlippedPieces> flipped;
    List<NodePiece> dead;
    List<FallsPiece> fallingP;

    System.Random random;//we can use using System

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        List<NodePiece> finishedUpdating = new List<NodePiece>();
        for (int i = 0; i < update.Count; i++)
        {
            NodePiece piece = update[i];
            if (!piece.UpdatePiece()) 
            {
                finishedUpdating.Add(piece);
            }
        }
        for (int i = 0; i < finishedUpdating.Count; i++)
        {
            NodePiece piece = finishedUpdating[i];
            FlippedPieces flip = getFlipped(piece);
            NodePiece flippedPiece = null;

            int x = (int)piece.index.x;
            fills[x] = Mathf.Clamp(fills[x] - 1, 0, width);


            List<Point> connected = isConnected(piece.index, true);
            bool wasFlipped = (flip != null);

            if (wasFlipped)//if we flipped to make this update
            {
                flippedPiece = flip.getOtherPiece(piece);
                AddPoint(ref connected, isConnected(flippedPiece.index, true));
            }
            if (connected.Count==0)//if we did not make a match
            {
                if (wasFlipped)//if we flipped
                {
                    FlipPieces(piece.index, flippedPiece.index, false);//flip back

                }
            }
            else//if we made a mutch
            {
                foreach(Point pnt in connected)//remove the node pieces at when connected
                {
                    FallsPiece(pnt);
                    Node node = getNodeAtPoint(pnt);
                    NodePiece nodePiece = node.getPiece();
                    if (nodePiece !=null)
                    {
                        nodePiece.gameObject.SetActive(false);
                        dead.Add(nodePiece);
                    }
                    node.SetPiece(null);
                }

                ApplyGravityToBoard();
            }

            flipped.Remove(flip);//remove the flip after update
            update.Remove(piece);
        }
    }

    void ApplyGravityToBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = (height - 1); y >=0; y--)
            {
                Point p = new Point(x, y);
                Node node = getNodeAtPoint(p);
                int val = getValueAtPoint(p);
                if (val!=0)
                {
                    continue;//if it is not a hole< do nothing
                }
                for (int ny = (y-1); ny>= -1 ; ny--)
                {
                    Point next = new Point(x, ny);
                    int nextVal = getValueAtPoint(next);
                    if (nextVal==0)
                    {
                        continue;
                    }
                    if (nextVal != -1)//if we did not hit an end? but it's not 0 then use this to fill the current hole
                    {
                        Node gotten = getNodeAtPoint(next);
                        NodePiece piece = gotten.getPiece();

                        //set the hole
                        node.SetPiece(piece);
                        update.Add(piece);

                        //replace the hole
                        gotten.SetPiece(null);
                    }
                    else//hit an end
                    {
                        //fill in the whole
                        int newVal = fillPiece();
                        NodePiece piece;
                        Point fallPoint = new Point(x, (-1-fills[x]));

                        if (dead.Count>0)
                        {
                            NodePiece revieved = dead[0];
                            revieved.gameObject.SetActive(true);
                            revieved.rect.anchoredPosition = getPositionFromPoint(fallPoint);
                            piece = revieved;

                            dead.RemoveAt(0);
                        }
                        else
                        {
                            GameObject obj = Instantiate(nodePiece, gameBoard);
                            NodePiece n = obj.GetComponent<NodePiece>();

                            piece = n;
                        }


                        piece.Initialize(newVal, p, pieces[newVal -1]);
                        piece.rect.anchoredPosition = getPositionFromPoint(fallPoint);

                        Node hole = getNodeAtPoint(p);
                        hole.SetPiece(piece);
                        ResetPiece(piece);
                        fills[x]++;
                    }
                    break;
                }
            }
        }
    }

    FlippedPieces getFlipped(NodePiece p)
    {
        FlippedPieces flip = null;
        for (int i = 0; i < flipped.Count; i++)
        {
            if (flipped[i].getOtherPiece(p) != null)
            {
                flip = flipped[i];
                break;
            }
        }
        return flip;

    }

    void StartGame()
    {
        fills = new int[width];
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());
        update = new List<NodePiece>();
        flipped = new List<FlippedPieces>();
        dead = new List<NodePiece>();
        fallingP = new List<FallsPiece>();

        InitializeBoard();
        VerifyBoard();
        InstantiateBoard();
    }

    void InitializeBoard()
    {
        board = new Node[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                board[x, y] = new Node((boardLayout.rows[y].row[x]) ? -1 : fillPiece(), new Point(x, y));
            }
        }
    }

    void VerifyBoard()
    {
        List<int> remove;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Point p = new Point(x, y);
                int val = getValueAtPoint(p);
                if (val<=0)
                {
                    continue;
                }

                remove = new List<int>();
                while (isConnected(p, true).Count>0)
                {
                    val = getValueAtPoint(p);
                    if (!remove.Contains(val))
                    {
                        remove.Add(val);
                    }
                    setValueAtPoint(p, newValue(ref remove));
                }
            }
        }
    }

    void InstantiateBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = getNodeAtPoint(new Point(x,y));

                int val = node.value;
                if (val<=0)
                {
                    continue;
                }
                GameObject p = Instantiate(nodePiece, gameBoard);
                NodePiece piece = p.GetComponent<NodePiece>();
                RectTransform rect = p.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(32 + (64 * x), -32 - (64 * y));
                piece.Initialize(val, new Point(x, y), pieces[val - 1]);
                node.SetPiece(piece);
            }
        }
    }

    public void ResetPiece(NodePiece piece)
    {
        piece.ResetPosition();
        update.Add(piece);
    }

    public void FlipPieces(Point one, Point two, bool main)
    {
        if (getValueAtPoint(one)<0)
        {
            return;
        }

        Node nodeOne = getNodeAtPoint(one);
        NodePiece pieceOne = nodeOne.getPiece();

        if (getValueAtPoint(two)>0)
        {
            Node nodeTwo = getNodeAtPoint(two);
            NodePiece pieceTwo = nodeTwo.getPiece();
            nodeOne.SetPiece(pieceTwo);
            nodeTwo.SetPiece(pieceOne);

            if (main)
            {
                flipped.Add(new FlippedPieces(pieceOne, pieceTwo));
            }
            
            update.Add(pieceOne);
            update.Add(pieceTwo);
        }
        else
        {
            ResetPiece(pieceOne);
        }
    }

    void FallsPiece(Point p)
    {
        List<FallsPiece> available = new List<FallsPiece>();
        for (int i = 0; i < fallingP.Count; i++)
        {
            if (!fallingP[i].falling)
            {
                available.Add(fallingP[i]);
            }
        }

        FallsPiece set = null;
        if (available.Count > 0)
        {
            set = available[0];
        }
        else
        {
            GameObject fall = GameObject.Instantiate(fallsPiece, fallsBoard);
            FallsPiece fPiece = fall.GetComponent<FallsPiece>();
            set = fPiece;
            fallingP.Add(fPiece);
        }

        int val = getValueAtPoint(p) - 1;
        if (set != null && val >= 0 && val < pieces.Length)
        {
            set.Initialize(pieces[val], getPositionFromPoint(p));
        }
    }

    List<Point> isConnected(Point p, bool main)
    {
        List<Point> connected = new List<Point>();
        int val = getValueAtPoint(p);
        Point[] directions =
        {
            Point.up,
            Point.right,
            Point.down,
            Point.left
        };

        foreach (Point dir in directions)//cheking if there are 2 or more same shapes
        {
            List<Point> line = new List<Point>();

            int same = 0;
            for (int i = 1; i < 3; i++)
            {
                Point check = Point.add(p, Point.mult(dir, i));
                if (getValueAtPoint(check)==val)
                {
                    line.Add(check);
                    same++;
                }
            }

            if(same > 1)//if there are more than 1 of the same shape
            {
                AddPoint(ref connected, line);//add these points overacing connected list
            }
        }

        for (int i = 0; i < 2; i++)//cheking if we are in the middle of two of the same shapes
        {
            List<Point> line = new List<Point>();

            int same = 0;
            Point[] check = { Point.add(p, directions[i]),Point.add(p, directions[i + 2]) };
            foreach(Point next in check)//check both sides of the piece, ig they are have the same value, add them to the list
            {
                if (getValueAtPoint(next) == val)
                {
                    line.Add(next);
                    same++;
                }
            }

            if (same>1)
            {
                AddPoint(ref connected, line);
            }
        }

        for (int i = 0; i < 4; i++)//checking for 2*2
        {
            List<Point> square = new List<Point>();

            int same = 0;
            int next = i + 1;
            if (next>=4)
            {
                next -= 4;
            }

            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[next]), Point.add(p, Point.add(directions[i],directions[next])) };
            foreach (Point pnt in check)//check all sides of the piece, ig they are have the same value, add them to the list
            {
                if (getValueAtPoint(pnt) == val)
                {
                    square.Add(pnt);
                    same++;
                }
            }

            if (same > 2)
            {
                AddPoint(ref connected, square);
            }
        }

        if (main)//checking for other matches along the current match
        {
            for (int i = 0; i < connected.Count; i++)
            {
                AddPoint(ref connected, isConnected(connected[i], false));
            }
        }

        /*if(connected.Count>0)
        {
            connected.Add(p);
        }*/

        return connected;
    }

    void AddPoint(ref List<Point> points, List<Point> add)
    {
        foreach (Point p in add)
        {
            bool doAdd = true;

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Equals(p))
                {
                    doAdd = false;
                    break;
                }
            }

            if (doAdd)
            {
                points.Add(p);
            }
        }
    }

    int fillPiece()
    {
        int val = 1;
        val = (random.Next(0, 100) / (100 / pieces.Length)) + 1;
        return val;
    }

    int getValueAtPoint(Point p)
    {
        if (p.x < 0||p.x>=width||p.y<0||p.y>=height)
        {
            return -1;
        }
        return board[p.x, p.y].value;
    }

    void setValueAtPoint(Point p, int v)
    {
        board[p.x, p.y].value = v;
    }

    Node getNodeAtPoint(Point p)
    {
        return board[p.x, p.y];
    }

    int newValue(ref List<int> remove)
    {
        List<int> available = new List<int>();
        for (int i = 0; i < pieces.Length; i++)
        {
            available.Add(i + 1);
        }
        foreach (int i in remove)
        {
            available.Remove(i);
        }
        if (available.Count<=0)
        {
            return 0;
        }
        return available[random.Next(0, available.Count)];
    }

    string getRandomSeed()
    {
        string seed = "";
        string acceptableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*()";
        for (int i = 0; i < 20;i++ )
        {
            seed += acceptableChars[Random.Range(0, acceptableChars.Length)];
        }
        return seed;
    }

    public Vector2 getPositionFromPoint(Point p)
    {
        return new Vector2(32 + (64 * p.x), -32 - (64 * p.y));
    }
}

[System.Serializable]
public class Node
{
    public int value;//-1-hole,0-Amethyst,1-Argentum,2-Emerald,3-Gold,4-Obsidian,5-Ruby,6-Sapphire,7-Topaz
    public Point index;
    public NodePiece piece;

    public Node(int v, Point i)
    {
        value = v;
        index = i;
    }

    public void SetPiece(NodePiece p)
    {
        piece = p;
        value = (piece == null) ? 0 : piece.value;
        if (piece==null)
        {
            return;
        }
        piece.SetIndex(index);
    }

    public NodePiece getPiece()
    {
        return piece;
    }
}

[System.Serializable]
public class FlippedPieces
{
    public NodePiece one;
    public NodePiece two;

    public FlippedPieces(NodePiece o, NodePiece t)
    {
        one = o;
        two = t;
    }

    public NodePiece getOtherPiece(NodePiece p)
    {
        if (p==one)
        {
            return two;
        }
        else if (p==two)
        {
            return one;
        }
        else
        {
            return null;
        }
    }
}
