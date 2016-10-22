using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableTool.Code
{
    public static class GoTemplate
    {
        public const string CodeTemplate = @"package table

import . ""logger""

type {0}Ptr struct {{
    *{0}
}}

type {0}Manager struct {{
    array {0}Array
    items map[int64]*{0}Ptr
}}

func(this *{0}Manager) Source() string {{
    return ""{0}.bytes""
}}

func(this *{0}Manager) Key( item *{0} ) int64 {{
    return int64({1})
}}

func(this *{0}Manager) Count() int {{
    return len(this.array.Items)
}}

func(this *{0}Manager) Get( index int ) *{0} {{

    if index < this.Count() {{
        return this.array.Items[index]
    }}

    return nil
}}

func(this *{0}Manager) Find( key int64 ) *{0}Ptr {{

    if data, ok := this.items[key]; ok {{
        return data
    }}

    return nil
}}

func(this *{0}Manager) FindEx( key1, key2 int32 ) *{0}Ptr {{
    key := int64(key1)
    key <<= 32
    key |= int64(key2)

    return this.Find( key )
}}

func(this *{0}Manager) ForEach( start_idx, count int, callback func( *{0} ) bool ) {{

    var size int

    if size = this.Count(); 0 == size || start_idx >= size {{
        WARN( ""{0}Manager.ForEach: size: %d, offset: %d"", size, start_idx )
        return
    }}

    if count > 0 {{
       size = min( this.Count(), start_idx + count )
    }}

    for i := start_idx; i < size; i++ {{
       if !callback( this.array.Items[i] ) {{
            break
       }}
    }}
}}

func(this *{0}Manager) Load() bool {{

    if err := loadTable( this.Source(), &this.array ); err != nil {{
       FATAL( ""load Fail. %s %s"", this.Source(), err.Error() )
       return false
    }}

    for _, item := range( this.array.Items ) {{

       key := this.Key( item )

       if data, ok := this.items[key]; ok {{
           data.{0} = item
       }} else {{
            this.items[key] = &{0}Ptr{{ {0} : item }}
       }}
    }}

    return true
}}

var {0}ManagerInstance *{0}Manager

func Load{0}() bool {{

    if nil == {0}ManagerInstance {{
        {0}ManagerInstance = &{0}Manager{{ items : make(map[int64]*{0}Ptr) }}
    }}

    return {0}ManagerInstance.Load()
}}

func init() {{

    if AutoLoadTable( ""{0}"" ) {{
        Load{0}()
    }}
}}";
    }
}
