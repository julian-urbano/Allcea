// Copyright (C) 2014  Julián Urbano <urbano.julian@gmail.com>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jurbano.Allcea.Model
{
    [global::System.Diagnostics.DebuggerDisplay("Document:{Document}, Artist:{Artist}, Genre={Genre}")]
    public class Metadata
    {
        public string Document { get; protected set; }
        public string Artist { get; protected set; }
        public string Genre { get; protected set; }

        public Metadata(string doc, string artist, string genre)
        {
            this.Document = doc;
            this.Artist = artist;
            this.Genre = genre;
        }
    }
}
