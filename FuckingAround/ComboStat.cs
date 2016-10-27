﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuckingAround {
	public class ComboStat : astat {

		private List<astat> components = new List<astat>();
		public void AddComponent(astat stat) {
			components.Add(stat);
		}

		private double _base;
		public override double Base {
			get { return _base; }
			set { Update(() => _base = value); }
		}
		private double _additiveMultipliers;
		public override double AdditiveMultipliers {
			get { return _additiveMultipliers; }
			set { Update(() => _additiveMultipliers = value); }
		}
		public Stat AdditiveFuckYou { get { return new Stat() { AdditiveMultipliers = AdditiveMultipliers }; } }

		public class ValueUpdatedEventArgs : EventArgs {
			public double PrevValue;
			public ValueUpdatedEventArgs(double prevVal) {
				PrevValue = prevVal;
			}
		}

		private void Recalc() {
			Value = components
				.Select(s => new ComboStat(s, AdditiveFuckYou))
				.Aggregate(this.Base * (1 + AdditiveMultipliers), (a, b) => a + b.Value)
				* Multipliers.Aggregate(1.0, (a, b) => a * b);
		}

		public event EventHandler<ValueUpdatedEventArgs> ValueUpdated;
		private void Update(Action setter) {
			var prevVal = Value;
			setter();
			Recalc();
			if (ValueUpdated != null)
				ValueUpdated(this, new ValueUpdatedEventArgs(prevVal));
		}
		private T Update<T>(Func<T> setter) {
			var prevVal = Value;
			T r = setter();
			Recalc();
			if (ValueUpdated != null)
				ValueUpdated(this, new ValueUpdatedEventArgs(prevVal));
			return r;
		}

		private ObservableCollection<double> _multipliers = new ObservableCollection<double>();
		public override ICollection<double> Multipliers { get { return _multipliers; } }
		private void OnMultipliersChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.Action != NotifyCollectionChangedAction.Move) {
				Update(() => { });
			}
		}

		public ComboStat(StatType st) {
			StatType = st;
			_multipliers.CollectionChanged += OnMultipliersChanged;
		}

		public ComboStat(params astat[] stats) : this(stats as IEnumerable<astat>) { }
		public ComboStat(IEnumerable<astat> stats0, params astat[] stats) : this(stats0.Concat(stats)) { }
		public ComboStat(IEnumerable<astat> stats) {
			foreach (var that in stats) {
				if (this.StatType.Supports(that.StatType)) this.StatType = that.StatType;
				else if (!that.StatType.Supports(this.StatType)) throw new ArgumentException("Incompatible StatTyping");
				this.Base += that.Base;
				this.AdditiveMultipliers += that.AdditiveMultipliers;
				foreach (var m in that.Multipliers)
					this.Multipliers.Add(m);
				if (that is ComboStat)
					this.components.AddRange((that as ComboStat).components);
			}

			_multipliers.CollectionChanged += OnMultipliersChanged;
			Recalc();
		}
	}
}