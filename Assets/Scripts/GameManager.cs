using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Raavanan
{
    public class GameManager : MonoBehaviour
    {
        #region Variables
        [SerializeField]
        private int                     mMaxHeight = 15;
        [SerializeField]
        private int                     mMaxWidth = 17;
        [SerializeField]
        private Color                   mColor1;
        [SerializeField]
        private Color                   mColor2;
        [SerializeField]
        private Color                   mPlayerColor;
        [SerializeField]
        private Color                   mAppleColor;
        [SerializeField]
        private Transform               mCameraHolder;
        [SerializeField]
        private float                   mMoveRate = 0.5f;

        [SerializeField]
        private Text                    mCurrentScoreTxt;
        [SerializeField]
        private Text                    mHighScoreTxt;

        private float                   mTimer;
        private GameObject              mMapObject;
        private GameObject              mPlayerObject;
        private GameObject              mAppleObject;
        private GameObject              mTailParentObject;

        private Sprite                  mPlayerSprite;
        private SpriteRenderer          mMapRenderer;
        private SpriteRenderer          mPlayerRenderer;
        private SpriteRenderer          mAppleRenderer;

        private Node                    mPlayerNode;
        private Node                    mAppleNode;        
        private Node[,]                 mGrid;
        private List<Node>              mAvailableNodes = new List<Node>();
        private List<TailNode>          mTailNodes = new List<TailNode>();

        private bool                    mUp;
        private bool                    mDown;
        private bool                    mLeft;
        private bool                    mRight;
        private bool                    mIsGameOver;
        private bool                    mIsFirstInput;

        private int                     mCurrentScore;
        private int                     mHighScore;

        public UnityEvent               _OnStart;
        public UnityEvent               _OnGameover;
        public UnityEvent               _FirstInput;
        public UnityEvent               _OnScore;

        private Direction               mTargetDirection;
        private Direction               mCurrentDirection;
        public enum                     Direction
        {
            E_Up,
            E_Down,
            E_Left,
            E_Right
        }
        #endregion

        #region Init
        private void Start()
        {
            _OnStart.Invoke();
        }

        public void StartNewGame ()
        {
            ClearReferences();
            CreateMap();
            PlacePlayer();
            PlaceCamera();
            CreateApple();
            mIsGameOver = false;
            mHighScore = PlayerPrefs.GetInt("_highscore", 0);
            mCurrentScore = 0;
            _OnScore.Invoke();
        }

        private void ClearReferences ()
        {
            if (mMapObject != null)
            {
                Destroy(mMapObject);
            }
            if(mPlayerObject != null)
            {
                Destroy(mPlayerObject);
            }
            if (mAppleObject != null)
            {
                Destroy(mAppleObject);
            }            
            foreach (var Obj in mTailNodes)
            {
                if (Obj._Object != null)
                {
                    Destroy(Obj._Object);
                }                
            }
            mTailNodes.Clear();
            mAvailableNodes.Clear();
            mGrid = null;
        }

        private void CreateMap()
        {
            mMapObject = new GameObject("Map");
            mMapRenderer = mMapObject.AddComponent<SpriteRenderer>();
            mGrid = new Node[mMaxWidth, mMaxHeight];
            Texture2D InTexture = new Texture2D(mMaxWidth, mMaxHeight);
            #region Visual Representation
            for (int i = 0; i < mMaxWidth; i++)
            {
                for (int j = 0; j < mMaxHeight; j++)
                {
                    Vector3 InTexturePos = Vector3.zero;
                    InTexturePos.x = i;
                    InTexturePos.y = j;
                    Node InNode = new Node(i, j, InTexturePos);
                    mAvailableNodes.Add(InNode);
                    mGrid[i, j] = InNode;
                    if (i % 2 == 0)
                    {
                        if (j % 2 == 0)
                        {
                            InTexture.SetPixel(i, j, mColor1);
                        }
                        else
                        {
                            InTexture.SetPixel(i, j, mColor2);
                        }
                    }
                    else
                    {
                        if (j % 2 == 0)
                        {
                            InTexture.SetPixel(i, j, mColor2);
                        }
                        else
                        {
                            InTexture.SetPixel(i, j, mColor1);
                        }
                    }
                }
            }
            InTexture.filterMode = FilterMode.Point;
            InTexture.Apply();
            #endregion
            Rect InRect = new Rect(0, 0, mMaxWidth, mMaxHeight);
            Sprite InSprite = Sprite.Create(InTexture, InRect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
            mMapRenderer.sprite = InSprite;
        }

        private void PlacePlayer()
        {
            mPlayerObject = new GameObject("Player");
            mPlayerRenderer = mPlayerObject.AddComponent<SpriteRenderer>();
            mPlayerSprite = CreateSprite(mPlayerColor);
            mPlayerRenderer.sprite = mPlayerSprite;
            mPlayerRenderer.sortingOrder = 1;
            mPlayerNode = GetNode(3, 3);
            PlacePlayerObject(mPlayerObject, mPlayerNode._WorldPos);
            mPlayerObject.transform.localScale = Vector3.one * 1.2f;
            mTailParentObject = new GameObject("Tail Parent");
        }

        private void PlaceCamera ()
        {
            Node InNode = GetNode(mMaxWidth / 2, mMaxHeight / 2);
            Vector3 InPos = InNode._WorldPos;
            InPos += Vector3.one * 0.5f;
            mCameraHolder.transform.position = InPos;
        }

        private void CreateApple ()
        {
            mAppleObject = new GameObject("Apple");
            mAppleRenderer = mAppleObject.AddComponent<SpriteRenderer>();
            mAppleRenderer.sprite = CreateSprite(mAppleColor);
            mAppleRenderer.sortingOrder = 1;
            RandomlyPlaceApple();
        }
        #endregion

        #region Update
        private void Update()
        {
            if (mIsGameOver)
            {
                if (Input.GetKeyDown (KeyCode.R))
                {
                    _OnStart.Invoke();
                }
                return;
            }
            GetInput();
            SetPlayerDirection();
            if (mIsFirstInput)
            {
                mTimer += Time.deltaTime;
                if (mTimer > mMoveRate)
                {
                    mTimer = 0;
                    mCurrentDirection = mTargetDirection;
                    MovePlayer();
                }
            }
            else
            {
                mIsFirstInput = (mUp || mDown || mLeft || mRight);
                if (mIsFirstInput)
                {
                    _FirstInput.Invoke();
                }
            }
        }

        private void GetInput()
        {
            mUp = Input.GetButtonDown("Up");
            mDown = Input.GetButtonDown("Down");
            mRight = Input.GetButtonDown("Right");
            mLeft = Input.GetButtonDown("Left");
        }

        private void SetPlayerDirection ()
        {
            if (mUp)
            {
                SetDirection(Direction.E_Up);
            }
            else if (mDown)
            {
                SetDirection(Direction.E_Down);
            }
            else if (mRight)
            {
                SetDirection(Direction.E_Right);
            }
            else if (mLeft)
            {
                SetDirection(Direction.E_Left);
            }
        }

        private void SetDirection (Direction pDirection)
        {
            if (!IsOpposite (pDirection))
            {
                mTargetDirection = pDirection;
            }
        }

        private void MovePlayer ()
        {
            int InX = 0, InY = 0;
            switch (mCurrentDirection)
            {
                case Direction.E_Up:
                    InY = 1;
                    break;
                case Direction.E_Down:  
                    InY = -1;
                    break;
                case Direction.E_Right:
                    InX = 1;
                    break;
                case Direction.E_Left:
                    InX = -1;
                    break;
            }
            Node InTargetNode = GetNode(mPlayerNode._x + InX, mPlayerNode._y + InY);
            if (InTargetNode == null)
            {
                _OnGameover.Invoke();
            }
            else
            {
                if (IsTailNode(InTargetNode))
                {
                    _OnGameover.Invoke();
                }
                else
                {
                    bool InIsScore = false;
                    if (InTargetNode == mAppleNode)
                    {
                        InIsScore = true;
                    }
                    Node InPreviousNode = mPlayerNode;
                    mAvailableNodes.Add(mPlayerNode);

                    if (InIsScore)
                    {
                        mTailNodes.Add(CreateTileNode(InPreviousNode._x, InPreviousNode._y));
                        mAvailableNodes.Remove(InPreviousNode);
                    }
                    MoveTail();
                    PlacePlayerObject(mPlayerObject, InTargetNode._WorldPos);
                    mPlayerNode = InTargetNode;
                    mAvailableNodes.Remove(mPlayerNode);
                    if (InIsScore)
                    {
                        mCurrentScore++;
                        if (mCurrentScore > mHighScore)
                        {
                            mHighScore = mCurrentScore;
                            PlayerPrefs.SetInt("_highscore", mHighScore);
                        }
                        _OnScore.Invoke();
                        if (mAvailableNodes.Count > 0)
                        {
                            RandomlyPlaceApple();
                        }
                        else
                        {
                            // You Won!!!
                        }
                    }
                }
            }
        }

        private void MoveTail ()
        {
            Node InPreviousNode = null;
            for(int i = 0; i < mTailNodes.Count; i++)
            {
                TailNode InTailNode = mTailNodes[i];
                mAvailableNodes.Add(InTailNode._Node);
                if (i == 0)
                {
                    InPreviousNode = InTailNode._Node;
                    InTailNode._Node = mPlayerNode;
                }
                else
                {
                    Node InPreviousNode1 = InTailNode._Node;
                    InTailNode._Node = InPreviousNode;
                    InPreviousNode = InPreviousNode1;
                }
                mAvailableNodes.Remove(InTailNode._Node);
                PlacePlayerObject(InTailNode._Object, InTailNode._Node._WorldPos);
            }
        }
        #endregion

        #region Utilities
        public void GameOver ()
        {
            mIsGameOver = true;
            mIsFirstInput = false;
        }

        public void UpdateScore ()
        {
            mCurrentScoreTxt.text = mCurrentScore.ToString ();
            mHighScoreTxt.text = mHighScore.ToString();
        }

        private bool IsOpposite (Direction pDirection)
        {
            switch (pDirection)
            {
                case Direction.E_Down:
                    return (mCurrentDirection == Direction.E_Up);
                case Direction.E_Up:
                    return (mCurrentDirection == Direction.E_Down);
                case Direction.E_Right:
                    return (mCurrentDirection == Direction.E_Left);
                case Direction.E_Left:
                    return (mCurrentDirection == Direction.E_Right);
            }
            return false;
        }

        private bool IsTailNode (Node pNode)
        {
            for (int i = 0; i < mTailNodes.Count; i++)
            {
                if (mTailNodes[i]._Node == pNode)
                {
                    return true;
                }
            }
            return false;
        }

        private void PlacePlayerObject (GameObject pObject, Vector3 pPosition)
        {
            pPosition += Vector3.one * 0.5f;
            pObject.transform.position = pPosition;
        }

        private void RandomlyPlaceApple ()
        {
            int InRandomValue = UnityEngine.Random.Range(0, mAvailableNodes.Count);
            Node InNode = mAvailableNodes[InRandomValue];
            PlacePlayerObject(mAppleObject, InNode._WorldPos);
            mAppleNode = InNode;
        }

        private Node GetNode (int pX, int pY)
        {
            if (pX < 0 || pX > mMaxWidth - 1 || pY < 0 || pY > mMaxHeight - 1)
            {
                return null;
            }
            return mGrid[pX, pY];
        }

        private TailNode CreateTileNode (int pX, int pY)
        {
            TailNode InTailNode = new TailNode();
            InTailNode._Node = GetNode(pX, pY);
            InTailNode._Object = new GameObject();
            InTailNode._Object.transform.parent = mTailParentObject.transform;
            InTailNode._Object.transform.position = InTailNode._Node._WorldPos;
            InTailNode._Object.transform.localScale = Vector3.one * 0.95f;
            SpriteRenderer InSpriteRenderer = InTailNode._Object.AddComponent<SpriteRenderer>();
            InSpriteRenderer.sprite = mPlayerSprite;
            InSpriteRenderer.sortingOrder = 1;
            return InTailNode;
        }

        private Sprite CreateSprite (Color pTargetColor)
        {
            Texture2D InTexture = new Texture2D(mMaxWidth, mMaxHeight);
            InTexture.SetPixel(0, 0, pTargetColor);
            InTexture.Apply();
            InTexture.filterMode = FilterMode.Point;
            Rect InRect = new Rect(0, 0, 1, 1);
            return Sprite.Create(InTexture, InRect, Vector3.one * 0.5f, 1.0f, 0, SpriteMeshType.FullRect);
        }
        #endregion
    }
}