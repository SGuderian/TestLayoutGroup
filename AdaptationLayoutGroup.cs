using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class XLayoutAdaptationNode : UIBehaviour, ILayoutElement, ILayoutGroup
{
    #region 成员
    /// <summary>
    /// 子节点的对齐方式
    /// </summary>
    public TextAnchor ChildAlignment = TextAnchor.UpperLeft;

    /// <summary>
    /// 子节点相对于容器边缘的偏移
    /// </summary>
    public RectOffset Padding = new RectOffset();

    /// <summary>
    /// 子节点相对于容器边缘的偏移(最小值)
    /// </summary>
    public RectOffset MinPadding = new RectOffset();

    /// <summary>
    /// 子节点相对于容器边缘的偏移（实际使用值）
    /// </summary>
    [HideInInspector]
    public RectOffset TmpPadding = new RectOffset();

    /// <summary>
    /// 间隙
    /// </summary>
    public Vector2 Spacing = Vector2.zero;

    /// <summary>
    /// 最小间隙
    /// </summary>
    public Vector2 MinSpacing = Vector2.zero;

    /// <summary>
    /// 真实使用的间隙
    /// </summary>
    private Vector2 TmpSpacing = Vector2.zero;

    /// <summary>
    /// 是否忽略非激活的
    /// </summary>
    public bool IgnoreUnActive = true;

    /// <summary>
    /// 方向
    /// </summary>
    public XLayoutRule.Direction Direction = XLayoutRule.Direction.Vertical;

    /// <summary>
    /// 顺序
    /// </summary>
    public XLayoutRule.Order Order = XLayoutRule.Order.Positive;

    /// <summary>
    /// 拉伸
    /// </summary>
    public bool Stretch = false;

    /// <summary>
    /// 总宽高
    /// </summary>
    protected float TotalWidth, TotalHeight;

    /// <summary>
    /// 最小大小
    /// </summary>
    public Vector2 MinSize = Vector2.zero;

    /// <summary>
    /// 容器RectTransform
    /// </summary>
    [System.NonSerialized]
    protected RectTransform m_Rect;
    public RectTransform rectTransform
    {
        get
        {
            if (m_Rect == null)
                m_Rect = GetComponent<RectTransform>();
            return m_Rect;
        }
    }

    /// <summary>
    /// 用于存储子节点队列
    /// </summary>
    [System.NonSerialized]
    protected List<RectTransform> UsingChildren = new List<RectTransform>();

    /// <summary>
    /// 用于存储所有子节点队列
    /// </summary>
    [System.NonSerialized]
    protected List<RectTransform> AllChildren = new List<RectTransform>();
    #endregion 成员

    #region 成员函数
    public  void SetAllDirty()
    {
        this.CalculateLayoutChildren();
        this.CalculateUsingLayoutChildren();
        this.CaculateLayoutContainerSize();
        this.SetCellsAlongAxis();
    }

    /// <summary>
    /// 更新布局
    /// </summary>
    public virtual void SetDirty()
    {
        if (!isActiveAndEnabled)
            return;

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    /// <summary>
    /// 计算容器动态间距的大小
    /// </summary>
    protected virtual void CaculateLayoutContainerSize()
    {
        Vector2 vec2 = Vector2.zero;

        TotalWidth = 0.0f;
        TotalHeight = 0.0f;
        float maxWidth = 0.0f;
        float maxHeight = 0.0f;
        TmpSpacing = Spacing;
        TmpPadding.left = Padding.left;
        TmpPadding.right = Padding.right;
        TmpPadding.top = Padding.top;
        TmpPadding.bottom = Padding.bottom;
        foreach (RectTransform item in UsingChildren)
        {
            Vector2 size = item.rect.size;
            TotalWidth += size.x;
            TotalHeight += size.y;
            maxWidth = Mathf.Max(maxWidth, size.x);
            maxHeight = Mathf.Max(maxHeight, size.y);
        }

        if (Direction == XLayoutRule.Direction.Horizontal)
        {
            float difWidth = TotalWidth + TmpPadding.horizontal + TmpSpacing.x * (UsingChildren.Count - 1) - rectTransform.rect.width;
            if (difWidth > 0)
            {
                if (TmpPadding.horizontal > 0)
                {
                    int leftDif = TmpPadding.left - (int)(difWidth / (TmpPadding.horizontal | 1) * TmpPadding.left);
                    TmpPadding.left = MinPadding.left < leftDif ? leftDif : MinPadding.left;

                    int rightDif = TmpPadding.right - (int)(difWidth / (TmpPadding.horizontal | 1) * TmpPadding.right);
                    TmpPadding.right = MinPadding.right < rightDif ? rightDif : MinPadding.right;

                }
                TmpSpacing.x = (rectTransform.rect.width - TotalWidth - TmpPadding.horizontal) / (UsingChildren.Count - 1);
                TmpSpacing.x = Mathf.Max(TmpSpacing.x, MinSpacing.x);
            }
            
        }

        else if (Direction == XLayoutRule.Direction.Vertical)
        {
            float difHeight = TotalHeight + TmpPadding.vertical + TmpSpacing.y * (UsingChildren.Count - 1) - rectTransform.rect.height;
            if (difHeight > 0)
            {
                if (TmpPadding.horizontal > 0)
                {

                    int topDif = TmpPadding.top - (int)(difHeight / (TmpPadding.vertical | 1) * TmpPadding.top);
                    TmpPadding.top = MinPadding.top < topDif ? topDif : MinPadding.top;

                    int bottomDif = TmpPadding.bottom - (int)(difHeight / (TmpPadding.vertical | 1) * TmpPadding.bottom);
                    TmpPadding.bottom = MinPadding.bottom < bottomDif ? bottomDif : MinPadding.bottom;

                }
                TmpSpacing.y = (rectTransform.rect.height - TotalHeight - TmpPadding.vertical) / (UsingChildren.Count - 1);
                TmpSpacing.y = Mathf.Max(TmpSpacing.y, MinSpacing.y);
            }
           
        }    
    }

    /// <summary>
    /// 计算Cells位置
    /// </summary>
    /// <param name="axis">轴</param>
    protected virtual void SetCellsAlongAxis()
    {

        Vector2 cellSize = Vector2.zero;

        Vector2 lastOffest = Vector2.zero;

        /*设置Cell的位置*/
        for (int i = 0; i < UsingChildren.Count; i++)
        {
            float positionX = 0;
            float positionY = 0;

            //获取子节点PerferSize
            cellSize = GetPerferSize(UsingChildren[i]);

            Vector2 startOffset = CalulateStartOffest(cellSize);

            switch (Direction)
            {
                case XLayoutRule.Direction.Horizontal:
                    positionX = (int)lastOffest.x + startOffset.x;
                    positionY += startOffset.y;
                    break;
                case XLayoutRule.Direction.Vertical:
                    positionY = (int)lastOffest.y + startOffset.y;
                    positionX += startOffset.x;

                    break;
            }

            lastOffest.x += (cellSize[0] + TmpSpacing[0]);
            lastOffest.y += (cellSize[1] + TmpSpacing[1]);

            SetChildAlongAxis(UsingChildren[i], 0, positionX, cellSize[0]);
            SetChildAlongAxis(UsingChildren[i], 1, positionY, cellSize[1]);
        }
    }

    /// <summary>
    /// 获取Item大小
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    protected virtual Vector2 GetPerferSize(RectTransform trans)
    {
        Vector2 size = trans.rect.size;

        if (Stretch == false)
            return size;
        Vector2 contentSize = rectTransform.rect.size;

        if (Direction == XLayoutRule.Direction.Horizontal)
            return new Vector2(size.x, contentSize.y - TmpPadding.vertical);

        else if (Direction == XLayoutRule.Direction.Vertical)
            return new Vector2(contentSize.x - TmpPadding.horizontal, size.y);

        return size;
    }

    /// <summary>
    /// 设置item的位置
    /// </summary>
    /// <param name="rect">RectTransform</param>
    /// <param name="axis">轴</param>
    /// <param name="pos">相对的距离</param>
    /// <param name="size">item 的大小</param>
    protected void SetChildAlongAxis(RectTransform rect, int axis, float pos, float size)
    {
        if (rect == null)
            return;

        RectTransform.Edge edge = axis == 0 ? RectTransform.Edge.Left : RectTransform.Edge.Top;
        if (Direction == XLayoutRule.Direction.Horizontal)
        {
            if (axis == 0 && Order == XLayoutRule.Order.Reverse)
                edge = RectTransform.Edge.Right;
            else if (axis == 0 && Order == XLayoutRule.Order.Positive)
                edge = RectTransform.Edge.Left;
        }
        else if (Direction == XLayoutRule.Direction.Vertical)
        {
            if (axis == 1 && Order == XLayoutRule.Order.Reverse)
                edge = RectTransform.Edge.Bottom;
            else if (axis == 1 && Order == XLayoutRule.Order.Positive)
                edge = RectTransform.Edge.Top;
        }

        /*相对于父节点的停靠位置*/
        rect.SetInsetAndSizeFromParentEdge(edge, pos, size);
    }

    /// <summary>
    /// 计算来自于停靠，对齐的坐标偏移
    /// </summary>
    protected virtual Vector2 CalulateStartOffest(Vector2 size)
    {
        /*计算Align上的偏移*/
        Vector2 requiredSpace = Vector2.zero;
        if (Direction == XLayoutRule.Direction.Horizontal)
        {
            requiredSpace = new Vector2(
                TotalWidth + (UsingChildren.Count - 1) * TmpSpacing.x,
                size.y
                );
        }
        else if (Direction == XLayoutRule.Direction.Vertical)
        {
            requiredSpace = new Vector2(
              size.x,
              TotalHeight + (UsingChildren.Count - 1) * TmpSpacing.y
              );
        }


        Vector2 startOffset = new Vector2(
                GetStartOffset(0, requiredSpace.x),
                GetStartOffset(1, requiredSpace.y)
                );

        return startOffset;
    }

    /// <summary>
    /// 计算开始位置的预留空间偏移
    /// </summary>
    /// <param name="axis">方向</param>
    /// <param name="requiredSpaceWithoutPadding">内容所占的实际大小</param>
    /// <returns></returns>
    protected virtual float GetStartOffset(int axis, float requiredSpaceWithoutPadding)
    {
        float requiredSpace = requiredSpaceWithoutPadding + (axis == 0 ? TmpPadding.horizontal : TmpPadding.vertical);
        float availableSpace = rectTransform.rect.size[axis];
        float surplusSpace = availableSpace - requiredSpace;
        float alignmentOnAxis = 0;
        if (axis == 0)
            alignmentOnAxis = ((int)ChildAlignment % 3) * 0.5f;
        else
            alignmentOnAxis = ((int)ChildAlignment / 3) * 0.5f;

        return (axis == 0 ? TmpPadding.left : TmpPadding.top) + surplusSpace * alignmentOnAxis;
    }
    
    /// <summary>
    /// 计算子节点个数。无过滤
    /// </summary>
    protected virtual void CalculateLayoutChildren()
    {
        AllChildren.Clear();

        //子节点列表
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            var rect = rectTransform.GetChild(i) as RectTransform;
            //过滤非激活的
            if (rect == null)
                continue;
            AllChildren.Add(rect);
        }
    }

    /// <summary>
    /// 计算子节点个数
    /// </summary>
    protected virtual void CalculateUsingLayoutChildren()
    {
        UsingChildren.Clear();
        var toIgnoreList = XListPool<Component>.New();
        //子节点列表
        for (int i = 0; i < AllChildren.Count; i++)
        {
            var rect = AllChildren[i];// RectTransform;
            //过滤非激活的
            if (rect == null || (IgnoreUnActive && !rect.gameObject.activeInHierarchy))
                continue;


            //判断是否过滤布局
            rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);

            if (toIgnoreList.Count == 0)
            {
                UsingChildren.Add(rect);
                continue;
            }
            //忽略多个，只要有一个不忽略就加入计算
            for (int j = 0; j < toIgnoreList.Count; j++)
            {
                var ignorer = (ILayoutIgnorer)toIgnoreList[j];
                if (!ignorer.ignoreLayout)
                {
                    UsingChildren.Add(rect);
                    break;
                }
            }
        }
        XListPool<Component>.Free(toIgnoreList);
    }
    #endregion

    #region 布局接口
    public virtual float minWidth
    {
        get
        {
            return MinSize.x;
        }
    }

    public virtual float minHeight
    {
        get
        {
            return MinSize.y;
        }
    }

    public virtual float preferredWidth
    {
        get
        {
            return rectTransform.rect.size.x;
        }
    }

    public virtual float flexibleWidth
    {
        get
        {
            return rectTransform.rect.size.x;
        }
    }

    public virtual float preferredHeight
    {
        get
        {
            return rectTransform.rect.size.y;
        }
    }

    public virtual float flexibleHeight
    {
        get
        {
            return rectTransform.rect.size.y;
        }
    }

    public virtual int layoutPriority { get { return 0; } }

    public void CalculateLayoutInputHorizontal()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            CalculateLayoutChildren();
#endif
        CalculateUsingLayoutChildren();
    }

    public void CalculateLayoutInputVertical()
    {
        
    }

    public virtual void SetLayoutHorizontal()
    {
        if (Direction != XLayoutRule.Direction.Horizontal)
            return;

        this.CaculateLayoutContainerSize();
        this.SetCellsAlongAxis();
    }

    public virtual void SetLayoutVertical()
    {
        if (Direction != XLayoutRule.Direction.Vertical)
            return;

        this.CaculateLayoutContainerSize();
        this.SetCellsAlongAxis();
    }
    #endregion

    #region 事件函数触发

    protected override void Awake()
    {
        this.CalculateLayoutChildren();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        this.SetDirty();
    }

    protected virtual void OnTransformChildrenChanged()
    {
        this.CalculateLayoutChildren();
        this.SetDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetDirty();
    }
#endif
    #endregion

}
