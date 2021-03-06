// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AdaptiveCards.Rendering.Wpf
{
    public static class AdaptiveActionSetRenderer
    {
        public static FrameworkElement Render(AdaptiveActionSet actionSet, AdaptiveRenderContext context)
        {
            var outerActionSet = new Grid();

            if (!context.Config.SupportsInteractivity)
                return outerActionSet;

            outerActionSet.Style = context.GetStyle("Adaptive.Container");

            // Keep track of ContainerStyle.ForegroundColors before Container is rendered
            AdaptiveRenderArgs parentRenderArgs = context.RenderArgs;
            AdaptiveRenderArgs elementRenderArgs = new AdaptiveRenderArgs(parentRenderArgs);

            RenderActions(outerActionSet, actionSet.Actions, context);

            return outerActionSet;
        }

        public static void AddRenderedActions(Grid uiContainer, IList<AdaptiveAction> actions, AdaptiveRenderContext context)
        {
            RenderActions(uiContainer, actions, context);
            AddShowCardsViewsToRoot(context);
        }

        public static void RenderActions(Grid uiContainer, IList<AdaptiveAction> actions, AdaptiveRenderContext context)
        {
            if (!context.Config.SupportsInteractivity)
                return;

            ActionsConfig actionsConfig = context.Config.Actions;
            int maxActions = actionsConfig.MaxActions;
            List<AdaptiveAction> actionsToProcess = GetActionsToProcess(actions, maxActions, context);

            if (actionsToProcess.Any())
            {
                var uiActionBar = new UniformGrid();

                if (actionsConfig.ActionsOrientation == ActionsOrientation.Horizontal)
                    uiActionBar.Columns = actionsToProcess.Count();
                else
                    uiActionBar.Rows = actionsToProcess.Count();

                uiActionBar.HorizontalAlignment = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), actionsConfig.ActionAlignment.ToString());
                uiActionBar.VerticalAlignment = VerticalAlignment.Bottom;
                uiActionBar.Style = context.GetStyle("Adaptive.Actions");

                // For vertical, we want to subtract the top margin of the first button
                int topMargin = actionsConfig.ActionsOrientation == ActionsOrientation.Horizontal
                    ? context.Config.GetSpacing(actionsConfig.Spacing)
                    : context.Config.GetSpacing(actionsConfig.Spacing) - actionsConfig.ButtonSpacing;

                uiActionBar.Margin = new Thickness(0, topMargin, 0, 0);

                uiContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                Grid.SetRow(uiActionBar, uiContainer.RowDefinitions.Count - 1);
                uiContainer.Children.Add(uiActionBar);

                bool isInline = (actionsConfig.ShowCard.ActionMode == ShowCardActionMode.Inline);

                int iPos = 0;

                // See if all actions have icons, otherwise force the icon placement to the left
                IconPlacement oldConfigIconPlacement = actionsConfig.IconPlacement;
                bool allActionsHaveIcons = true;
                foreach (AdaptiveAction action in actionsToProcess)
                {
                    if (string.IsNullOrEmpty(action.IconUrl))
                    {
                        allActionsHaveIcons = false;
                        break;
                    }
                }

                if (!allActionsHaveIcons)
                {
                    actionsConfig.IconPlacement = IconPlacement.LeftOfTitle;
                }

                foreach (AdaptiveAction action in actionsToProcess)
                {
                    // add actions
                    var uiAction = (Button)context.Render(action);

                    if (uiAction == null)
                    {
                        continue;
                    }

                    if (actionsConfig.ActionsOrientation == ActionsOrientation.Horizontal)
                    {
                        if (uiActionBar.Children.Count > 0) // don't apply left margin to the first item
                            uiAction.Margin = new Thickness(actionsConfig.ButtonSpacing, 0, 0, 0);
                    }
                    else
                    {
                        uiAction.Margin = new Thickness(0, actionsConfig.ButtonSpacing, 0, 0);
                    }


                    if (actionsConfig.ActionsOrientation == ActionsOrientation.Horizontal)
                        Grid.SetColumn(uiAction, iPos++);

                    uiActionBar.Children.Add(uiAction);

                    if (action is AdaptiveShowCardAction showCardAction)
                    {
                        // Only support 1 level of showCard
                        if (isInline && context.CardDepth == 1)
                        {
                            Grid uiShowCardContainer = new Grid();
                            uiShowCardContainer.Style = context.GetStyle("Adaptive.Actions.ShowCard");
                            uiShowCardContainer.DataContext = showCardAction;
                            uiShowCardContainer.Margin = new Thickness(0, actionsConfig.ShowCard.InlineTopMargin, 0, 0);
                            uiShowCardContainer.Visibility = Visibility.Collapsed;

                            // render the card
                            var uiShowCardWrapper = (Grid)context.Render(showCardAction.Card);
                            uiShowCardWrapper.Background = context.GetColorBrush("Transparent");
                            uiShowCardWrapper.DataContext = showCardAction;

                            // Remove the card padding
                            var innerCard = (Grid)uiShowCardWrapper.Children[0];
                            innerCard.Margin = new Thickness(0);

                            uiShowCardContainer.Children.Add(uiShowCardWrapper);

                            // Add to the list of show cards in context
                            context.ActionShowCards.Add(uiAction, uiShowCardContainer);
                            context.ActionShowCardsKeys.Enqueue(uiAction);
                        }
                    }
                }

                // Restore the iconPlacement for the context.
                actionsConfig.IconPlacement = oldConfigIconPlacement;
            }
        }

        /// <summary>
        /// adds showcards views to the root view
        /// </summary>
        /// <param name="context"></param>
        public static void AddShowCardsViewsToRoot(AdaptiveRenderContext context)
        {
            // Only handle Action show cards for the main card
            if (context.CardDepth == 1)
            {
                if (context.CardRoot is Grid cardRoot)
                {
                    if (cardRoot.Children.Count > 0 && cardRoot.Children[0] is Grid root)
                    {
                        // Define a new row to contain all the show cards
                        if (context.ActionShowCardsKeys.Count > 0)
                        {
                            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                        }

                        while (context.ActionShowCardsKeys.Count > 0)
                        {
                            Button uiButton = context.ActionShowCardsKeys.Dequeue();
                            FrameworkElement currentShowCard = context.ActionShowCards[uiButton];

                            Grid.SetRow(currentShowCard, root.RowDefinitions.Count - 1);
                            root.Children.Add(currentShowCard);

                            // Assign on click function to all button elements
                            uiButton.Click += (sender, e) =>
                            {
                                bool isCardCollapsed = (currentShowCard.Visibility != Visibility.Visible);

                                // Collapse all the show cards
                                foreach (KeyValuePair<Button, FrameworkElement> t in context.ActionShowCards)
                                {
                                    FrameworkElement showCard = t.Value;
                                    showCard.Visibility = Visibility.Collapsed;
                                }

                                // If current card is previously collapsed, show it
                                if (isCardCollapsed)
                                    currentShowCard.Visibility = Visibility.Visible;
                            };
                        }
                    }
                }
            }
        }

        private static List<AdaptiveAction> GetActionsToProcess(IList<AdaptiveAction> actions, int maxActions, AdaptiveRenderContext context)
        {
            // If the number of actions is bigger than maxActions, then log warning for it
            if (actions.Count > maxActions)
            {
                context.Warnings.Add(new AdaptiveWarning((int)AdaptiveWarning.WarningStatusCode.MaxActionsExceeded, "Some actions were not rendered due to exceeding the maximum number of actions allowed"));
            }

            // just take the first maxActions actions
            return actions.Take(maxActions).ToList();
        }
    }
}
