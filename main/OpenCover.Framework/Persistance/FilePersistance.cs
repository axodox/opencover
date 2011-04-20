﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using OpenCover.Framework.Model;

namespace OpenCover.Framework.Persistance
{
    /// <summary>
    /// This class is temporary until I decide 
    /// on a proper persistance framework.
    /// </summary>
    public class FilePersistance : IPersistance
    {
        private string _fileName;
        private CoverageSession _session;

        public void Initialise(string fileName)
        {
            _fileName = fileName;
            _session = new CoverageSession();
        }

        public void PersistModule(Module module)
        {
            var list = new List<Module>(_session.Modules ?? new Module[0]) {module};
            _session.Modules = list.ToArray();
        }

        public void Commit()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(CoverageSession), new[] { typeof(Module), typeof(Model.File), typeof(Class) });
                var fs = new FileStream(_fileName, FileMode.Create);
                var writer = new StreamWriter(fs, new UTF8Encoding());
                serializer.Serialize(writer, _session);
                writer.Close();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        public bool GetSequencePointsForFunction(string moduleName, int functionToken, out SequencePoint[] sequencePoints)
        {
            sequencePoints = new SequencePoint[0];
            if (_session.Modules == null) return false;
            var module = _session.Modules.Where(x => x.FullName == moduleName).FirstOrDefault();
            if (module == null) return false;
            foreach (var method in module.Classes.SelectMany(@class => @class.Methods.Where(method => method.MetadataToken == functionToken)))
            {
                if (method == null) continue;
                Debug.WriteLine(method.Name);
                sequencePoints = method.SequencePoints;
                return true;
            }
            return false;       
        }

        public void SaveVisitPoints(VisitPoint[] visitPoints)
        {
            var summary = from point in visitPoints
                          group point by point.UniqueId into counts
                          let count = counts.Count()
                          select new { point = counts.Key, Count = count };

            foreach (var sum in summary)
            {
                var sum1 = sum;
                foreach (var sequencePoint in from module in _session.Modules
                                              from @class in module.Classes
                                              from method in @class.Methods
                                              from sequencePoint in method.SequencePoints
                                              where sequencePoint.UniqueSequencePoint == sum1.point
                                              select sequencePoint)
                {
                    sequencePoint.VisitCount += sum.Count;
                }
            }
        }
    }
}
