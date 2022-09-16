using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Marketplace.Import.Helpers
{
    /// <summary>
    /// Следит за изменениями формы и изменяет размеры/положение контролов
    /// </summary>
    public class ResizeFormHelper
    {
        public static ResizeFormHelper Instance { get; } = new ResizeFormHelper();

        private Dictionary<Form, HashSet<Control>> _controls = new Dictionary<Form, HashSet<Control>>();
        private Dictionary<Control, Size> _sourceControlSize = new Dictionary<Control, Size>();
        private Dictionary<Control, Point> _sourceControlPosition = new Dictionary<Control, Point>();

        public void AddResizeControl(Control control, ResizeType resizeType = ResizeType.All)
        {
            Form form = control.FindForm();

            int offsetWidth = resizeType == ResizeType.Width || resizeType == ResizeType.All ? form.Width - control.Width : 0;
            int offsetHeight = resizeType == ResizeType.Height || resizeType == ResizeType.All ? form.Height - control.Height : 0;

            if (!_controls.TryGetValue(form, out HashSet<Control> controls))
                _controls[form] = controls = new HashSet<Control>();

            controls.Add(control);
            _sourceControlSize[control] = new Size(offsetWidth, offsetHeight);
            AddHandler(form);
        }

        public void AddFixControl(Control control, PositionType positionType = PositionType.All)
        {
            Form form = control.FindForm();

            int offsetX = positionType == PositionType.Left || positionType == PositionType.All ? form.Width - control.Location.X : 0;
            int offsetY = positionType == PositionType.Bottom || positionType == PositionType.All ? form.Height - control.Location.Y : 0;

            if (!_controls.TryGetValue(form, out HashSet<Control> controls))
                _controls[form] = controls = new HashSet<Control>();

            controls.Add(control);
            _sourceControlPosition[control] = new Point(offsetX, offsetY);
            AddHandler(form);
        }

        private HashSet<Form> _forms = new HashSet<Form>();
        private void AddHandler(Form form)
        {
            if (_forms.Contains(form))
                return;
            _forms.Add(form);

            form.Resize += (x, y) => { ResizeСhangesForm(form); };
        }

        private void ResizeСhangesForm(Form form)
        {
            if (!_controls.TryGetValue(form, out HashSet<Control> controls))
                return;

            foreach (var control in controls)
            {
                if (_sourceControlSize.TryGetValue(control, out Size size))
                {
                    if (size.Width != 0)
                        control.Width = form.Width - size.Width;

                    if (size.Height != 0)
                        control.Height = form.Height - size.Height;
                }

                if (_sourceControlPosition.TryGetValue(control, out Point point))
                {
                    Point newPoint = new Point();

                    if (point.X != 0)
                        newPoint.X = form.Width - point.X;

                    if (point.Y != 0)
                        newPoint.Y = form.Height - point.Y;

                    control.Location = newPoint;
                }
            }
        }
    }

    public enum PositionType
    {
        Left,
        Bottom,
        All
    }

    public enum ResizeType
    {
        Width,
        Height,
        All
    }
}
