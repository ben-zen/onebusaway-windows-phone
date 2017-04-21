/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Microsoft.EntityFrameworkCore;
using OneBusAway.Model.AppDataDataStructures;
using OneBusAway.Model.EventArgs;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;


namespace OneBusAway.Model
{
  public class AppDataModel : DbContext
  {

    #region Private Variables

    private const string _dbFileName = "storage.db";
    private bool initialized;
    private Object initializeLock;

    #endregion

    #region Properties

    public DbSet<FavoriteRoute> FavoriteRoutes { get; private set; }
    public DbSet<FavoriteStop> FavoriteStops { get; private set; }
    public DbSet<RecentRoute> RecentRoutes { get; private set; }
    public DbSet<RecentStop> RecentStops { get; private set; }
    #endregion

     #region Constructor/Initialize/Singleton

    public static AppDataModel Singleton = new AppDataModel();

    // Constructor is public for testing purposes
    public AppDataModel()
    {
      initialized = false;
      initializeLock = new Object();
    }

    #endregion
  }
}
