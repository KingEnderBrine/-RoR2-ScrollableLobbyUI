using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    public class ConstrainedScrollRect : ScrollRect
    {
        public Constraint scrollConstraint;
        public ScrollRect redirectConstrained;

        private void RedirectEvent<T>(Action<T> action) where T : IEventSystemHandler
        {
            if (redirectConstrained)
            {
                foreach (var component in redirectConstrained.GetComponents<T>())
                {
                    action(component);
                }
            }
        }

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            RedirectEvent<IInitializePotentialDragHandler>((redirectObject) => { redirectObject.OnInitializePotentialDrag(eventData); });
            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (scrollConstraint == Constraint.OnlyScroll)
            {
                RedirectEvent<IDragHandler>((redirectObject) => { redirectObject.OnDrag(eventData); });
            }
            else
            {
                base.OnDrag(eventData);
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (scrollConstraint == Constraint.OnlyScroll)
            {
                RedirectEvent<IBeginDragHandler>((redirectObject) => { redirectObject.OnBeginDrag(eventData); });
            }
            else
            {
                base.OnBeginDrag(eventData);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (scrollConstraint == Constraint.OnlyScroll)
            {
                RedirectEvent<IEndDragHandler>((redirectObject) => { redirectObject.OnEndDrag(eventData); });
            }
            else
            {
                base.OnEndDrag(eventData);
            }
        }

        public override void OnScroll(PointerEventData eventData)
        {
            if (scrollConstraint == Constraint.OnlyDrag)
            {
                RedirectEvent<IScrollHandler>((redirectObject) => { redirectObject.OnScroll(eventData); });
            }
            else
            {
                base.OnScroll(eventData);
            }
        }

        public enum Constraint
        {
            None,
            OnlyDrag,
            OnlyScroll
        }
    }
}
