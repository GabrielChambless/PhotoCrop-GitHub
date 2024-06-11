using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public enum TickTypes
    {
        PriorityOrder,
        InstantiationOrder,
        Simultaneous
    }

    public static TickManager Instance { get; private set; }

    public Coroutine TickCoroutine { get; private set; }
    public float tickDuration = 0.5f;
    private int tickCounter;

    private List<TickSubscriber> priorityOrderSubscribers = new List<TickSubscriber>();
    private List<TickSubscriber> instantiationOrderSubscribers = new List<TickSubscriber>();
    private List<TickSubscriber> simultaneousSubscribers = new List<TickSubscriber>();

    private int priorityIndex;
    private int instantiationIndex;

    private class TickSubscriber
    {
        public CellEntity Subscriber;
        public Func<CellEntity, IEnumerator> TickAction;
        public int TickOrder;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void StartTicking()
    {
        if (TickCoroutine != null)
        {
            Debug.LogWarning("The tick routine is already running.");
        }

        TickCoroutine = StartCoroutine(TickRoutine());
    }

    public void StopTicking()
    {
        if (TickCoroutine == null)
        {
            return;
        }

        tickCounter = 0;
        StopCoroutine(TickCoroutine);
        TickCoroutine = null;
    }

    private IEnumerator TickRoutine()
    {
        while (true)
        {
            if (!priorityOrderSubscribers.Any() && !instantiationOrderSubscribers.Any() && !simultaneousSubscribers.Any())
            {
                StopTicking();
                Debug.Log("There are no more subscribers; automatically stopping TickRoutine.");
                yield break;
            }

            bool tickActionPerformed = false;

            if (priorityIndex < priorityOrderSubscribers.Count)
            {
                yield return priorityOrderSubscribers.OrderBy(s => s.TickOrder).ElementAt(priorityIndex).TickAction(priorityOrderSubscribers[priorityIndex].Subscriber);
                priorityIndex++;
                tickActionPerformed = true;
            }
            else if (instantiationIndex < instantiationOrderSubscribers.Count)
            {
                yield return instantiationOrderSubscribers.ElementAt(instantiationIndex).TickAction(instantiationOrderSubscribers[instantiationIndex].Subscriber);
                instantiationIndex++;
                tickActionPerformed = true;
            }
            else if (simultaneousSubscribers.Any())
            {
                foreach (TickSubscriber subscriber in simultaneousSubscribers)
                {
                    yield return subscriber.TickAction(subscriber.Subscriber);
                }

                // Reset indices after simultaneous subscribers
                priorityIndex = 0;
                instantiationIndex = 0;
                tickActionPerformed = true;
            }

            if (!tickActionPerformed)
            {
                // Reset indices if no action was performed
                priorityIndex = 0;
                instantiationIndex = 0;

                // Perform the next available tick action
                if (priorityIndex < priorityOrderSubscribers.Count)
                {
                    yield return priorityOrderSubscribers.OrderBy(s => s.TickOrder).ElementAt(priorityIndex).TickAction(priorityOrderSubscribers[priorityIndex].Subscriber);
                    priorityIndex++;
                }
                else if (instantiationIndex < instantiationOrderSubscribers.Count)
                {
                    yield return instantiationOrderSubscribers.ElementAt(instantiationIndex).TickAction(instantiationOrderSubscribers[instantiationIndex].Subscriber);
                    instantiationIndex++;
                }
                else if (simultaneousSubscribers.Any())
                {
                    foreach (var subscriber in simultaneousSubscribers)
                    {
                        yield return subscriber.TickAction(subscriber.Subscriber);
                    }

                    // Reset indices after simultaneous subscribers
                    priorityIndex = 0;
                    instantiationIndex = 0;
                }
            }

            tickCounter++;
            Debug.Log($"Tick Counter: {tickCounter}");
            yield return new WaitForSeconds(tickDuration);
        }
    }

    public void Subscribe(CellEntity subscriber, Func<CellEntity, IEnumerator> tickAction, int tickOrder, TickTypes tickType)
    {
        if (subscriber == null)
        {
            Debug.LogWarning("The subscriber's EntityObject was null; cannot subscribe to TickManager.");
            return;
        }

        TickSubscriber tickSubscriber = new TickSubscriber
        {
            Subscriber = subscriber,
            TickAction = tickAction,
            TickOrder = tickOrder
        };

        switch (tickType)
        {
            case TickTypes.PriorityOrder:
                if (!priorityOrderSubscribers.Any(s => s.Subscriber == subscriber && s.TickAction == tickAction))
                {
                    priorityOrderSubscribers.Add(tickSubscriber);
                }
                break;

            case TickTypes.InstantiationOrder:
                if (!instantiationOrderSubscribers.Any(s => s.Subscriber == subscriber && s.TickAction == tickAction))
                {
                    instantiationOrderSubscribers.Add(tickSubscriber);
                }
                break;

            case TickTypes.Simultaneous:
                if (!simultaneousSubscribers.Any(s => s.Subscriber == subscriber && s.TickAction == tickAction))
                {
                    simultaneousSubscribers.Add(tickSubscriber);
                }
                break;
        }
    }

    public void Unsubscribe(CellEntity subscriber, Func<CellEntity, IEnumerator> tickAction, TickTypes tickType)
    {
        List<TickSubscriber> targetList = null;

        switch (tickType)
        {
            case TickTypes.PriorityOrder:
                targetList = priorityOrderSubscribers;
                break;
            case TickTypes.InstantiationOrder:
                targetList = instantiationOrderSubscribers;
                break;
            case TickTypes.Simultaneous:
                targetList = simultaneousSubscribers;
                break;
        }

        if (targetList == null)
        {
            return;
        }

        TickSubscriber subscriberToRemove = targetList.FirstOrDefault(s => s.Subscriber == subscriber && s.TickAction == tickAction);

        if (subscriberToRemove != null)
        {
            targetList.Remove(subscriberToRemove);
        }
    }
}

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//public class TickManager : MonoBehaviour
//{
//    public enum TickTypes
//    {
//        PriorityOrder,
//        InstantiationOrder,
//        Simultaneous
//    }

//    public static TickManager Instance { get; private set; }

//    public Coroutine TickCoroutine { get; private set; }
//    public float tickDuration = 1f;
//    private int tickCounter;

//    private List<TickSubscriber> priorityOrderSubscribers = new List<TickSubscriber>();
//    private List<TickSubscriber> instantiationOrderSubscribers = new List<TickSubscriber>();
//    private List<TickSubscriber> simultaneousSubscribers = new List<TickSubscriber>();

//    private int priorityIndex;
//    private int instantiationIndex;

//    private class TickSubscriber
//    {
//        public CellEntity Subscriber;
//        public Action<CellEntity> TickAction;
//        public int TickOrder;
//    }

//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//        else
//        {
//            Destroy(gameObject);
//            return;
//        }
//    }

//    public void StartTicking()
//    {
//        if (TickCoroutine != null)
//        {
//            Debug.LogWarning("The tick routine is already running.");
//        }

//        TickCoroutine = StartCoroutine(TickRoutine());
//    }

//    public void StopTicking()
//    {
//        if (TickCoroutine == null)
//        {
//            return;
//        }

//        tickCounter = 0;
//        StopCoroutine(TickCoroutine);
//        TickCoroutine = null;
//    }

//    private IEnumerator TickRoutine()
//    {
//        while (true)
//        {
//            if (!priorityOrderSubscribers.Any() && !instantiationOrderSubscribers.Any() && !simultaneousSubscribers.Any())
//            {
//                StopTicking();
//                Debug.Log("There are no more subscribers; automatically stopping TickRoutine.");
//                yield break;
//            }

//            bool tickActionPerformed = false;

//            if (priorityIndex < priorityOrderSubscribers.Count)
//            {
//                priorityOrderSubscribers.OrderBy(s => s.TickOrder).ElementAt(priorityIndex).TickAction?.Invoke(priorityOrderSubscribers[priorityIndex].Subscriber);
//                priorityIndex++;
//                tickActionPerformed = true;
//            }
//            else if (instantiationIndex < instantiationOrderSubscribers.Count)
//            {
//                instantiationOrderSubscribers.ElementAt(instantiationIndex).TickAction?.Invoke(instantiationOrderSubscribers[instantiationIndex].Subscriber);
//                instantiationIndex++;
//                tickActionPerformed = true;
//            }
//            else if (simultaneousSubscribers.Any())
//            {
//                foreach (var subscriber in simultaneousSubscribers)
//                {
//                    subscriber.TickAction?.Invoke(subscriber.Subscriber);
//                }

//                // Reset indices after simultaneous subscribers
//                priorityIndex = 0;
//                instantiationIndex = 0;
//                tickActionPerformed = true;
//            }

//            if (!tickActionPerformed)
//            {
//                // Reset indices if no action was performed
//                priorityIndex = 0;
//                instantiationIndex = 0;

//                // Perform the next available tick action
//                if (priorityIndex < priorityOrderSubscribers.Count)
//                {
//                    priorityOrderSubscribers.OrderBy(s => s.TickOrder).ElementAt(priorityIndex).TickAction?.Invoke(priorityOrderSubscribers[priorityIndex].Subscriber);
//                    priorityIndex++;
//                }
//                else if (instantiationIndex < instantiationOrderSubscribers.Count)
//                {
//                    instantiationOrderSubscribers.ElementAt(instantiationIndex).TickAction?.Invoke(instantiationOrderSubscribers[instantiationIndex].Subscriber);
//                    instantiationIndex++;
//                }
//                else if (simultaneousSubscribers.Any())
//                {
//                    foreach (var subscriber in simultaneousSubscribers)
//                    {
//                        subscriber.TickAction?.Invoke(subscriber.Subscriber);
//                    }

//                    // Reset indices after simultaneous subscribers
//                    priorityIndex = 0;
//                    instantiationIndex = 0;
//                }
//            }

//            tickCounter++;
//            Debug.Log($"Tick Counter: {tickCounter}");
//            yield return new WaitForSeconds(tickDuration);
//        }
//    }

//    public void Subscribe(CellEntity subscriber, Action<CellEntity> tickAction, int tickOrder, TickTypes tickType)
//    {
//        if (subscriber == null)
//        {
//            Debug.LogWarning("The subscriber's EntityObject was null; cannot subscribe to TickManager.");
//            return;
//        }

//        TickSubscriber tickSubscriber = new TickSubscriber
//        {
//            Subscriber = subscriber,
//            TickAction = tickAction,
//            TickOrder = tickOrder
//        };

//        switch (tickType)
//        {
//            case TickTypes.PriorityOrder:
//                if (!priorityOrderSubscribers.Any(s => s.Subscriber == subscriber && s.TickAction == tickAction))
//                {
//                    priorityOrderSubscribers.Add(tickSubscriber);
//                }
//                break;

//            case TickTypes.InstantiationOrder:
//                if (!instantiationOrderSubscribers.Any(s => s.Subscriber == subscriber && s.TickAction == tickAction))
//                {
//                    instantiationOrderSubscribers.Add(tickSubscriber);
//                }
//                break;

//            case TickTypes.Simultaneous:
//                if (!simultaneousSubscribers.Any(s => s.Subscriber == subscriber && s.TickAction == tickAction))
//                {
//                    simultaneousSubscribers.Add(tickSubscriber);
//                }
//                break;
//        }
//    }

//    public void Unsubscribe(CellEntity subscriber, Action<CellEntity> tickAction, TickTypes tickType)
//    {
//        List<TickSubscriber> targetList = null;

//        switch (tickType)
//        {
//            case TickTypes.PriorityOrder:
//                targetList = priorityOrderSubscribers;
//                break;
//            case TickTypes.InstantiationOrder:
//                targetList = instantiationOrderSubscribers;
//                break;
//            case TickTypes.Simultaneous:
//                targetList = simultaneousSubscribers;
//                break;
//        }

//        if (targetList == null)
//        {
//            return;
//        }

//        TickSubscriber subscriberToRemove = targetList.FirstOrDefault(s => s.Subscriber == subscriber && s.TickAction == tickAction);

//        if (subscriberToRemove != null)
//        {
//            targetList.Remove(subscriberToRemove);
//        }
//    }
//}