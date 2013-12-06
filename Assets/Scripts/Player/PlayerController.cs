using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PlayerController : MonoBehaviour
{
    public Action jumpEnd;
    public Action tackleEnd;
    public Action cutEnd;
    public Action catchEnd;

    private Transform m_transform;
    private Board m_board;

    private float m_speed = 2.5f;

    private List<Vector2> m_toMoveSquareList;

    private bool m_isFliped = false;

    private Player m_player;
    
    private exSprite m_sprite;
    public exSprite m_spriteShadow;

    private PlayerAnimation m_playerAnimation;

    private Vector2 m_index;

    public void init(Player pPlayer, Transform pTransform, Board pBoard)
    {
        m_transform = pTransform;
        m_board = pBoard;

        m_player = pPlayer;

        m_sprite = GetComponent<exSprite>();
        
        m_playerAnimation = GetComponent<PlayerAnimation>();

        //Set idle animation
        m_playerAnimation.playAnimation(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Idle);
    }

    public void move(List<Vector2> pToMoveSquareList)
    {
        m_playerAnimation.playAnimation(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Run);
        m_toMoveSquareList = pToMoveSquareList;

        StartCoroutine(moveToNextSquare());
    }

    public IEnumerator moveToNextSquare()
    {
        if (m_toMoveSquareList.Count > 0)
        {
            m_playerAnimation.playAnimation(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Run);

            Vector2 nextSquareIndex = m_toMoveSquareList[0];
            Vector2 direction = nextSquareIndex - m_player.Index;

            m_isFliped = direction.x != 0 && direction.x < 0;

            while ((new Vector2(transform.position.x, transform.position.z) - nextSquareIndex).sqrMagnitude > 0.05f)
            {
                transform.position += new Vector3(direction.x, 0, direction.y) * m_speed * Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }

            Index = nextSquareIndex;
            m_toMoveSquareList.RemoveAt(0);

            if (m_player.hasBall) ApplicationFactory.instance.m_messageBus.dispatchPlayerMovedToTile(m_player);
            else StartCoroutine(moveToNextSquare());
        }
        else
        {
            m_playerAnimation.playAnimation(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Idle);
            ApplicationFactory.instance.m_messageBus.dispatchPlayerMoveEnded(m_player);
        }
    }

    public void performTackle(Vector2 pTackleToIndex)
    {
        m_playerAnimation.playAnimation(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Tackle);
        StartCoroutine(tackleCoroutine(pTackleToIndex));
    }

    private IEnumerator tackleCoroutine(Vector2 pTackleToIndex)
    {
        Vector2 direction = pTackleToIndex - Index;
        while ((new Vector2(transform.position.x, transform.position.z) - pTackleToIndex).sqrMagnitude > 0.005f)
        {
            transform.position += new Vector3(direction.x, 0, direction.y) * 2.5f * Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        Index = pTackleToIndex;
        if (tackleEnd != null) tackleEnd();

        m_playerAnimation.playAnimation(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Idle);
    }

    public void performJump(Vector2 pJumpToIndex)
    {
        List<string> animationNameList = new List<string>();
        animationNameList.Add(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Jump);
        animationNameList.Add(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Idle);

        m_playerAnimation.playAnimation(animationNameList);
        StartCoroutine(jumpCoroutine(pJumpToIndex));
    }

    private IEnumerator jumpCoroutine(Vector2 pJumpToIndex)
    {
        Vector2 direction = pJumpToIndex - Index;
        while ((new Vector2(transform.position.x, transform.position.z) - pJumpToIndex).sqrMagnitude > 0.005f)
        {
            transform.position += new Vector3(direction.x, 0, direction.y) * 2.5f * Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
        
        Index = pJumpToIndex;
        if (jumpEnd != null) jumpEnd();
    }

    public void performCut()
    {
        m_playerAnimation.playAnimation(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Jump);
        m_playerAnimation.animationFinished += onCutAnimationEnd;
    }

    private void onCutAnimationEnd()
    {
        m_playerAnimation.animationFinished -= onCutAnimationEnd;
        if (cutEnd != null) cutEnd();
    }

    public void performCatch()
    {
        m_playerAnimation.playAnimation(m_player.team.ID + (m_player.isGK ? "_gk_" : "_player_") + PlayerAnimationIds.Catch);
        if (catchEnd != null) catchEnd();
    }

    private void onCatchAnimationEnd()
    {
        m_playerAnimation.animationFinished -= onCatchAnimationEnd;
        if (catchEnd != null) catchEnd();
    }

    #region PROPERTIES

    public Vector2 Index
    {
        set
        {
            transform.position = new Vector3(value.x, 0, value.y);

            m_index = value;

            //If we have the ball, set the ball index
            if (m_player.m_ball)
            {
                m_player.m_ball.Index = m_index;
            }
        }

        get
        {
            return m_index;
        }
    }

    public bool isFliped
    {
        set
        {
            //Set the value
            m_isFliped = value;

            //Set the sprite scale
            float scaleX = Mathf.Abs(m_sprite.scale.x) * (m_isFliped ? -1 : 1);
            float scaleY = m_sprite.scale.y;

            m_sprite.scale = new Vector2(scaleX, scaleY);
            m_spriteShadow.scale = new Vector2(scaleX, scaleY);
        }

        get
        {
            return m_isFliped;
        }
    }

    public Vector2 nextMoveTileIndex
    {
        get { return m_toMoveSquareList[0]; }
    }

    public bool isMoveEnded
    {
        get { return m_toMoveSquareList.Count == 0; }
    }

    #endregion
}

public struct TackleInfo
{
    public bool m_isDribble;

    public Player m_jumpPlayer;
    public Player m_tacklePlayer;

    public Vector2 m_jumpToIndex;
    public Vector2 m_tackleToIndex;
}

public struct CutInfo
{
    public bool m_isPass;
    public Player m_cutPlayer;
}

public struct CatchInfo
{
    public bool m_isGoal;
    public Player m_catchPlayer;
}