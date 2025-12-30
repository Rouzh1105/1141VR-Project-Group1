using UnityEngine;

/*
==============================
[DragAndDrop] - Script placed on every piece in the board.
==============================
*/
class DragAndDrop : MonoBehaviour {
    PlayerSwitch ps;
    private bool dragging = false;
    private float distance;
    private Piece this_piece;

    [SerializeField]
    private Board board;
    private Vector3 mousePos;

    void Start() {
        ps = GameObject.FindObjectOfType<PlayerSwitch>();
        this_piece = GetComponent<Piece>(); // Get piece's component
    }

    void Update() {
        if (dragging) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //Vector3 rayPoint = ray.GetPoint(distance);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f,3))
            {
                Vector3 hitPoint = hit.point;
                //Debug.Log("Hit point: " + hitPoint);
            }
            // Update piece's dragging position, we try to place it as close as we can to the mouse
            //transform.position = new Vector3(rayPoint.x - 0.5f, 2.7f, rayPoint.z);
            //transform.rotation = new Quaternion(0, 0, 0, 0);
            mousePos = new Vector3(hit.point.x - 0.5f, 0, hit.point.z - 0.5f);

            // Hover the square this piece could go id we drop it
            if (board.use_hover) {
                Square closest_square = board.getClosestSquare(mousePos);
                board.hoverClosestSquare(closest_square);
            }
            Square cur = board.getClosestSquare(this.transform.position);
            board.hoverSelfSquare(cur);
        }
    }

    void OnMouseDown() {
        if (ps.isThird) return;
        // If it's my turn
        if (board.cur_turn == this_piece.team) {
            //GetComponent<Rigidbody>().isKinematic = true;
            // Set distance between the mouse & this piece
            distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (board.use_hover) {
                board.hoverValidSquares(this_piece);
            }       
            dragging = true; // Start dragging
        }
    }
 
    void OnMouseUp() {
        if (dragging) {
            GetComponent<Rigidbody>().isKinematic = true;
            // Get closest square & try to move the piece to it
            Square closest_square = board.getClosestSquare(mousePos);
            this_piece.movePiece(closest_square);

            if (board.use_hover) board.resetHoveredSquares();
            dragging = false; // Stop dragging
        }
    }
}