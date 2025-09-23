using UnityEditor;
using UnityEngine;
using Utility;
namespace Editor
{
	[CustomPropertyDrawer(typeof(BlendData<>))]
	public class BlendDataDrawer : PropertyDrawer
	{
		private const float CanvasSize = 200f;
		private const float PointSize = 8f;
		private const float Padding = 10f;

		private int _draggingIndex = -1;
		private bool _isDragging = false;
		private static bool _isExpanded;
		
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!_isExpanded) return 10;
			SerializedProperty pointsProperty = property.FindPropertyRelative("Points");
			float listHeight = EditorGUI.GetPropertyHeight(pointsProperty, true);
			float boolHeight = 12;
			return Mathf.Max(CanvasSize, listHeight)+ boolHeight +10 + Padding * 2;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			
			Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			
			_isExpanded = EditorGUI.Foldout(labelRect, _isExpanded, "Blend Data", true);
			
			if (_isExpanded)
			{
				SerializedProperty pointsProperty = property.FindPropertyRelative("Points");
				SerializedProperty boolProperty = property.FindPropertyRelative("PersistentOnSceneLoad");

				// Rect labelRect = new Rect(position.x, position.y+10, position.width, EditorGUIUtility.singleLineHeight);
				// EditorGUI.LabelField(labelRect, label);
				float yOffset = labelRect.yMax + Padding;

				Rect canvasRect = new Rect(position.x, yOffset, CanvasSize, CanvasSize);
				Rect boolRect = new Rect(canvasRect.xMax + Padding, position.y+10, position.width - CanvasSize - Padding * 2, 12);
				Rect listRect = new Rect(canvasRect.xMax + Padding, yOffset + 6, position.width - CanvasSize - Padding * 2, position.height - yOffset - 6);

				DrawGrid(property, canvasRect, pointsProperty);

				DrawBool(boolRect, boolProperty);
				
				DrawList(listRect, pointsProperty);
				property.serializedObject.ApplyModifiedProperties();
			}
		}
		private void DrawGrid(SerializedProperty property, Rect canvasRect, SerializedProperty pointsProp)
		{

			GUI.Box(canvasRect, GUIContent.none);
			DrawGrid(canvasRect);
			
			Event @event = Event.current;
			Vector2 mousePos = @event.mousePosition;

			if (pointsProp.isArray)
			{
				for (int i = 0; i < pointsProp.arraySize; i++)
				{
					SerializedProperty pointProperty = pointsProp.GetArrayElementAtIndex(i);
					SerializedProperty locationProperty = pointProperty.FindPropertyRelative("Point");
					SerializedProperty colorProperty = pointProperty.FindPropertyRelative("Color");

					Vector2 loc = locationProperty.vector2Value;
					Vector2 normalized = (loc + Vector2.one) * 0.5f; // assuming location in -1 to 1 range
					Vector2 guiPos = new Vector2(
						canvasRect.x + normalized.x * canvasRect.width,
						canvasRect.y + (1 - normalized.y) * canvasRect.height
					);

					Rect pointRect = new Rect(guiPos.x - PointSize / 2, guiPos.y - PointSize / 2, PointSize, PointSize);
					EditorGUI.DrawRect(pointRect, colorProperty.colorValue);

					if (@event.type == EventType.MouseDown && pointRect.Contains(mousePos))
					{
						_draggingIndex = i;
						@event.Use();
						Undo.RecordObject(property.serializedObject.targetObject, "Move Point");
						_isDragging = true;
					}

					if (@event.type == EventType.MouseUp && _draggingIndex == i)
					{
						_draggingIndex = -1;
						@event.Use();
						_isDragging = false;
					}

					if (@event.type == EventType.MouseDrag && _draggingIndex == i)
					{
						Vector2 local = new Vector2(
							Mathf.Clamp01((mousePos.x - canvasRect.x) / canvasRect.width),
							Mathf.Clamp01((mousePos.y - canvasRect.y) / canvasRect.height)
						);

						Vector2 newLoc = (local * 2f) - Vector2.one;
						newLoc.y = -newLoc.y; // Flip Y

						locationProperty.vector2Value = newLoc.normalized;
						property.serializedObject.ApplyModifiedProperties();
						@event.Use();
					}
				}
			}
		}
		private void DrawList(Rect listRect, SerializedProperty pointsProperty)
		{
			GUIContent content = new GUIContent("Points");
			EditorGUI.BeginProperty(listRect,content, pointsProperty);
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(listRect, pointsProperty, content, true);
			
			if(EditorGUI.EndChangeCheck())
			{
				if (!_isDragging)
				{
					NormalizePoints(pointsProperty);
					SetDefaultColor(pointsProperty);
				}
			}
			
			EditorGUI.EndProperty();
		}

		private void DrawBool(Rect boolRect, SerializedProperty boolProperty)
		{
			GUIContent content = new GUIContent("Persistent On SceneChange");
			EditorGUI.BeginProperty(boolRect,content, boolProperty);
			EditorGUI.PropertyField(boolRect, boolProperty, content, true);
			EditorGUI.EndProperty();
		}
		
		private void NormalizePoints(SerializedProperty pointsProperty)
		{
			for (var i = 0; i < pointsProperty.arraySize; i++)
			{
				SerializedProperty pointProperty = pointsProperty.GetArrayElementAtIndex(i);
				SerializedProperty locationProperty = pointProperty.FindPropertyRelative("Point");

				locationProperty.vector2Value = locationProperty.vector2Value.normalized;
			}
		}
		
		private void SetDefaultColor(SerializedProperty pointsProperty)
		{
			for (var i = 0; i < pointsProperty.arraySize; i++)
			{
				SerializedProperty pointProperty = pointsProperty.GetArrayElementAtIndex(i);
				SerializedProperty colorProperty = pointProperty.FindPropertyRelative("Color");

				if (colorProperty.colorValue.a < 1)
				{
					Color color = colorProperty.colorValue;
					color.a = 1;
					if (color is { r: 0, g: 0, b: 0 })
					{
						color = Random.ColorHSV(0, 1, 0.5f, 1, 1, 1);
					}

					colorProperty.colorValue = color;
				}
			}
		}

		private void DrawGrid(Rect rect)
		{
			Handles.color = Color.gray;
			for (int i = 1; i < 10; i++)
			{
				float x = rect.x + i * rect.width / 10f;
				float y = rect.y + i * rect.height / 10f;
				Handles.DrawLine(new Vector2(x, rect.y), new Vector2(x, rect.yMax));
				Handles.DrawLine(new Vector2(rect.x, y), new Vector2(rect.xMax, y));
			}

			Handles.color = Color.white;
			Handles.DrawLine(new Vector2(rect.center.x, rect.y), new Vector2(rect.center.x, rect.yMax));
			Handles.DrawLine(new Vector2(rect.x, rect.center.y), new Vector2(rect.xMax, rect.center.y));
		}
	}
}