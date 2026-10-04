import { createContext, useContext, useEffect, useState } from "react";

const CartContext = createContext(null);

export function CartProvider({ children }) {
  const [cart, setCart] = useState(() => {
    try { return JSON.parse(localStorage.getItem("cart")) || []; }
    catch { return []; }
  });

  useEffect(() => localStorage.setItem("cart", JSON.stringify(cart)), [cart]);

  function addToCart(product, quantity = 1) {
    setCart(current => {
      const old = current.find(x => x.id === product.id);
      if (old) {
        return current.map(x =>
          x.id === product.id
            ? { ...x, quantity: Math.min(x.quantity + quantity, product.stock) }
            : x
        );
      }
      return [...current, {
        id: product.id,
        title: product.title,
        price: product.price,
        thumbnail: product.thumbnail,
        stock: product.stock,
        quantity
      }];
    });
  }

  function updateQuantity(id, quantity) {
    setCart(current => current.map(x =>
      x.id === id
        ? { ...x, quantity: Math.max(1, Math.min(quantity, x.stock)) }
        : x
    ));
  }

  function removeFromCart(id) {
    setCart(current => current.filter(x => x.id !== id));
  }

  function clearCart() { setCart([]); }

  const count = cart.reduce((s, x) => s + x.quantity, 0);
  const total = cart.reduce((s, x) => s + x.price * x.quantity, 0);

  return (
    <CartContext.Provider value={{ cart, count, total, addToCart, updateQuantity, removeFromCart, clearCart }}>
      {children}
    </CartContext.Provider>
  );
}

export const useCart = () => useContext(CartContext);