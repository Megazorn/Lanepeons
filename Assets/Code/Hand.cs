using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;
using System;

public class Hand : MonoBehaviour {

    public enum HandDirectionLayout
    {
        Right = 1,
        Left = -1
    }

    public CardPresenterController cardPresenter;
    public Deck deck;
    public PlayerSide side;
    public HandDirectionLayout directionLayout;

    public void PlayNetworkCard(string data)
    {
        NetworkAction action = JsonUtility.FromJson<NetworkAction>(data);

        Card newCard = null;
        if (action.cardChampionId != -1)
            newCard = deck.GetChampionCardById(action.cardChampionId);
        else
            newCard = deck.GetRandomChampionCard();

        newCard.Owner = side == PlayerSide.Friendly ? GameWorld.Instance.FriendlyPlayer : GameWorld.Instance.EnemyPlayer;
        newCard.onPlayActionChain.InjectVariables(JsonUtility.FromJson<NetworkAction>(data).variables);
        newCard.OnCardPlay(cardPresenter, null);
    }

    private Card potentialCard;
    private List<Card> cardsInHand;

    private bool cardPlayInProgress = false;

    private IEnumerator DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            DrawCard();
            yield return new WaitForSeconds(0.75f);
        }
    }

    private void DrawCard()
    {
        Card newCard = deck.GetRandomChampionCard();
        cardsInHand.Add(newCard);
        newCard.Owner = side == PlayerSide.Friendly ? GameWorld.Instance.FriendlyPlayer : GameWorld.Instance.EnemyPlayer;
        newCard.transform.SetParent(transform, false);
        newCard.transform.localPosition = new Vector3(20, 0, 0);

        UpdateCardsPosition();
    }

    private void UpdateCardsPosition()
    {
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            cardsInHand[i].transform.DOMove(transform.position + new Vector3(i * 0.4f * (int)directionLayout, 0, -i * 0.01f), 0.25f);
        }
    }

    private void PlayCard()
    {
        potentialCard.OnCardPlay(cardPresenter, OnCardPlayComplete);
        cardPlayInProgress = true;
    }

    private void OnCardPlayComplete()
    {
        Card tempCard = potentialCard;

        potentialCard.OnHoverExit();
        cardsInHand.Remove(potentialCard);
        potentialCard = null;
        UpdateCardsPosition();
        cardPlayInProgress = false;

        DrawCard();

        //test remote card
        NetworkAction list = new NetworkAction();
        if (tempCard.GetType() == typeof(ChampionCard))
            list.cardChampionId = ((ChampionCard)tempCard).championData.Id;
        else
            list.cardChampionId = -1;
        list.variables = tempCard.onPlayActionChain.ExtractVariables();
        SocketIOClient.Send(JsonUtility.ToJson(list));
    }

    #region UnityFunctions

    private IEnumerator Start()
    {
        cardsInHand = new List<Card>();
        yield return new WaitForSeconds(4);
        StartCoroutine(DrawCards(5));
    }

    private void Update()
    {
        if (cardPlayInProgress)
            return;

        if(Input.GetMouseButtonDown(0))
        {
            if(null != potentialCard)
            {
                PlayCard();
            }
        }
    }

    private void FixedUpdate()
    {
        if (cardPlayInProgress)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit info;
        if (Physics.Raycast(ray, out info))
        {
            Card card = info.transform.GetComponent<Card>();
            if (null != card && cardsInHand.Contains(card))
            {
                if (potentialCard != card)
                {
                    card.OnHoverEnter();

                    if (null != potentialCard)
                    {
                        potentialCard.OnHoverExit();
                    }
                }

                potentialCard = card;
            }
        }
        else
        {
            if(null != potentialCard)
            {
                potentialCard.OnHoverExit();
            }
            potentialCard = null;
        }
    }

    #endregion
}
