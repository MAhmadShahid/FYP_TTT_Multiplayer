using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class StageVisualScript : MonoBehaviour
{
    [SerializeField] GameObject _gridLineSquare, _gridLineContainer;

    Vector3 _topLeftCorner = new Vector3(-7.5f, 0.5f, 0);
    Vector3 _bottomRightCorner = new Vector3(0, 0.5f, -7.5f);

    int stageLength = 15;
    float padding = .5f;

    float lineWidth = .2f;

    public void CreateGridLines(int gridSize)
    {
        float totalLineCount = gridSize - 1;
        float excludePadding = stageLength - padding - padding;
        float basePosition = excludePadding / gridSize;
        lineWidth = gridSize >= 7 ? .05f : lineWidth;

        for (int count = 1; count <= totalLineCount; count++)
        {
            

            float initialPosition = basePosition * count;
            initialPosition += padding;
            UtilityClass.LogMessages($"Count: {count}; Position: {initialPosition}");

            // vertical lines
            Vector3 verticalLinePosition = _topLeftCorner + new Vector3(initialPosition, 0, 0);
            var verticalLine = Instantiate(_gridLineSquare, _gridLineContainer.transform);
            verticalLine.transform.localPosition = verticalLinePosition;
            verticalLine.transform.localScale = new Vector3(excludePadding, .1f, lineWidth);

            // horizontal lines
            Vector3 horizontalLinePosition = _bottomRightCorner + new Vector3(0, 0, initialPosition);
            var horizontalLine = Instantiate(_gridLineSquare, _gridLineContainer.transform);
            horizontalLine.transform.localPosition = horizontalLinePosition;
            horizontalLine.transform.localScale = new Vector3(lineWidth, .1f, excludePadding);
        }
    }

}
