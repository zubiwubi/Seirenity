using System;
using System.Collections.Generic;
using UnityEngine;

public class UIViewService : MonoBehaviour
	{
		public UIView CurrentView { get; private set; }

		private UIView[] views;
		private readonly Dictionary<Type, UIView> viewsDictionary = new();

		private readonly Stack<UIView> history = new();

		private void Awake()
		{
			SetupViewDictionary();
		}

		private void SetupViewDictionary()
		{
			views = UnityEngine.Object.FindObjectsByType<UIView>(FindObjectsSortMode.None);
			viewsDictionary.Clear();

			bool defaultViewFound = false;

			foreach (UIView view in views)
			{
				view.Initialize();
				view.Hide();

				viewsDictionary.Add(view.GetType(), view);

				if (!view.DefaultView) { continue; }

				if (defaultViewFound)
				{
					Debug.LogWarning("UIViewManager: Multiple default views found in current scene");
				}
				else
				{
					defaultViewFound = true;
					Show(view.GetType());
				}
			}

			if (!defaultViewFound)
			{
				Debug.LogWarning("UIViewManager: No default views found in current scene");
			}
		}

		public T GetView<T>() where T : UIView
		{
			if (viewsDictionary.TryGetValue(typeof(T), out UIView view))
			{
				return view as T;
			}

			return null;
		}

		public void Show<T>(bool remember = true) where T : UIView
		{
			Show(typeof(T), remember);
		}

		private void Show(Type viewType, bool remember = true)
		{
			if (!viewsDictionary.ContainsKey(viewType)) { return; }
			if (CurrentView != null && CurrentView.GetType() == viewType) { return; }

			if (CurrentView != null)
			{
				if (remember) { history.Push(CurrentView); }

				CurrentView.Hide();
			}

			CurrentView = viewsDictionary[viewType];
			CurrentView.Show();
		}

		public void ShowLast()
		{
			if (history.Count != 0)
			{
				Show(history.Pop().GetType(), false);
			}
		}
		
		public UIView GetViewByName(string fullTypeName)
		{
			if (string.IsNullOrEmpty(fullTypeName)) return null;

			Type type = null;
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = asm.GetType(fullTypeName, false);
				if (type != null) break;
			}

			if (type == null) return null;

			viewsDictionary.TryGetValue(type, out var view);
			return view;
		}
		
		public void ShowByName(string fullTypeName, bool remember = true)
		{
			if (string.IsNullOrEmpty(fullTypeName)) return;

			Type type = null;
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = asm.GetType(fullTypeName, false);
				if (type != null) break;
			}

			if (type == null) return;

			Show(type, remember);
		}
	}
