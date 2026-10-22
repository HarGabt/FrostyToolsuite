using Frosty.Core.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
//using System.Windows.Forms;

namespace ScalableEmitterEditorPlugin
{
    [TemplatePart(Name = PART_StackCanvas, Type = typeof(Canvas))]
    [TemplatePart(Name = PART_ProcessorBox, Type = typeof(Border))]
    [TemplatePart(Name = PART_ProcessorText, Type = typeof(TextBlock))]
    [TemplatePart(Name = PART_EvaluatorBox, Type = typeof(Border))]
    [TemplatePart(Name = PART_EvaluatorText, Type = typeof(TextBlock))]
    public class EmitterStackItem : UserControl
    {

        #region -- Part Names --

        private const string PART_StackCanvas = "PART_StackCanvas";
        private const string PART_ProcessorBox = "PART_ProcessorBox";
        private const string PART_ProcessorText = "PART_ProcessorText";
        private const string PART_EvaluatorBox = "PART_EvaluatorBox";
        private const string PART_EvaluatorText = "PART_EvaluatorText";

        #endregion

        private Border processorBox;
        private Border evaluatorBox;

        #region -- Constructors --

        static EmitterStackItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EmitterStackItem), new FrameworkPropertyMetadata(typeof(EmitterStackItem)));
        }

        public EmitterStackItem()
        {
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            processorBox = GetTemplateChild(PART_ProcessorBox) as Border;
            evaluatorBox = GetTemplateChild(PART_EvaluatorBox) as Border;
        }

    }
}
