﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using VisonProcess.Core.Extentions;

namespace VisonProcess.Core.Mvvm
{
    public partial class ProcessModel : ObservableObject
    {
        public ProcessModel()
        {
            Init();
            //OperationsMenu = new OperationsMenuViewModel(this);
        }

        private void Init()
        {
            Connections.WhenAdded(c =>
            {
                c.Input.IsConnected = true;
                c.Output.IsConnected = true;

                c.Input.Value = c.Output.Value;

                c.Output.ValueObservers.Add(c.Input);
            })
         .WhenRemoved(c =>
         {
             var ic = Connections.Count(con => con.Input == c.Input || con.Output == c.Input);
             var oc = Connections.Count(con => con.Input == c.Output || con.Output == c.Output);
             if (ic == 0)
             {
                 c.Input.IsConnected = false;
             }

             if (oc == 0)
             {
                 c.Output.IsConnected = false;
             }

             c.Output.ValueObservers.Remove(c.Input);
         });

            Operations.WhenAdded(x =>
            {
                x.Input.WhenRemoved(RemoveConnection);

                //if (x is CalculatorInputOperationViewModel ci)
                //{
                //    ci.Output.WhenRemoved(RemoveConnection);
                //}

                void RemoveConnection(ConnectorModel i)
                {
                    var c = Connections.Where(con => con.Input == i || con.Output == i).ToArray();
                    c.ForEach(con => Connections.Remove(con));
                }
            })
           .WhenRemoved(x =>
           {
               foreach (var input in x.Input)
               {
                   DisconnectConnector(input);
               }

               foreach (var output in x.Output)
               {
                   DisconnectConnector(output);
               }
           });
        }

        #region Properties

        private NodifyObservableCollection<OperationModel> _operations = new();
        private OperationModel? _selectedOperation;
        private NodifyObservableCollection<OperationModel> _selectedOperations = new();

        public NodifyObservableCollection<ConnectionModel> Connections { get; } = new();

        public NodifyObservableCollection<OperationModel> Operations
        {
            get => _operations;
            set => SetProperty(ref _operations, value);
        }

        public PendingConnectionModel PendingConnection { get; set; } = new();

        public NodifyObservableCollection<OperationModel> SelectedOperations
        {
            get => _selectedOperations;
            set
            {
                SetProperty(ref _selectedOperations, value);
                SelectedOperation = value?.FirstOrDefault();
            }
        }

        public OperationModel? SelectedOperation
        {
            get => _selectedOperation;
            set => SetProperty(ref _selectedOperation, value);
        }

        #endregion Properties

        #region Commands

        //不能与 CanCreateConnection 重名？？
        internal static bool IsCanCreateConnection(ConnectorModel source, ConnectorModel? target)
    => target == null || (source != target && source.Operation != target.Operation && source.IsInput != target.IsInput && source.ValueType == target.ValueType);

        private bool CanCreateConnection()
        {
            return IsCanCreateConnection(PendingConnection.Source, PendingConnection.Target);
        }

        private bool CanGroupSelection()
        {
            return SelectedOperations.Count > 0;
        }

        [RelayCommand(CanExecute = nameof(CanCreateConnection))]
        private void CreateConnection()
        {
            CreateConnection(PendingConnection.Source, PendingConnection.Target);
        }

        private void CreateConnection(ConnectorModel source, ConnectorModel? target)
        {
            if (target == null)
            {
                PendingConnection.IsVisible = false;
                //OperationsMenu.OpenAt(PendingConnection.TargetLocation);
                //OperationsMenu.Closed += OnOperationsMenuClosed;
                return;
            }

            var input = source.IsInput ? source : target;
            var output = target.IsInput ? source : target;
            PendingConnection.IsVisible = false;
            DisconnectConnector(input);

            //ValueType 相等时才连接，才可赋值
            //if (input.ValueType == output.ValueType)
            //{
            Connections.Add(new ConnectionModel
            {
                Input = input,
                Output = output
            });
            //}
        }

        [RelayCommand]
        private void DeleteSelection()
        {
            SelectedOperations.ForEach(o => Operations.Remove(o));
        }

        [RelayCommand]
        private void DisconnectConnector(ConnectorModel connector)
        {
            var connections = Connections.Where(c => c.Input == connector || c.Output == connector);
            connections.ForEach(c => Connections.Remove(c));
        }

        //[RelayCommand]
        //private void DeletLineConnection(ConnectionModel connection)
        //{
        //    connection.Input.IsConnected = false;
        //    connection.Output.IsConnected = false;
        //    Connections.Remove(connection);
        //}

        [RelayCommand(CanExecute = nameof(CanGroupSelection))]
        private void GroupSelection()
        {
            var bounding = SelectedOperations.GetBoundingBox(50);

            Operations.Add(new OperationGroupModel
            {
                Title = "Operations",
                Location = bounding.Location,
                GroupSize = new Size(bounding.Width, bounding.Height)
            });
        }

        [RelayCommand]
        private void StartConnection()
        {
            PendingConnection.IsVisible = true;
        }

        #endregion Commands
    }
}