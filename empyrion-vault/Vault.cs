using System;
using System.Collections.Generic;
using Eleon.Modding;

namespace empyrion_vault
{
  class Vault
  {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<ItemStack> Items { get; set; }
  }
}
