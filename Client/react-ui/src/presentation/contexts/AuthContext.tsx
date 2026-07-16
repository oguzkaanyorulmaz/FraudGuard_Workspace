import React, { createContext, useContext, useState, useEffect } from 'react';

interface AuthUser {
    token: string;
    username: string;
    role: number;
    roleName: string;
}

interface AuthContextType {
    user: AuthUser | null;
    login: (username: string, password: string) => Promise<boolean>;
    register: (username: string, email: string, password: string, role: number) => Promise<boolean>;
    logout: () => void;
    isLoggedIn: boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

export const useAuth = () => {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error('useAuth must be used within AuthProvider');
    return ctx;
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<AuthUser | null>(null);

    useEffect(() => {
        const stored = localStorage.getItem('fraudguard_user');
        if (stored) {
            try {
                setUser(JSON.parse(stored));
            } catch {
                localStorage.removeItem('fraudguard_user');
            }
        }
    }, []);

    const login = async (username: string, password: string): Promise<boolean> => {
        try {
            const res = await fetch('http://localhost:5217/api/Auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });
            const json = await res.json();
            if (json.isSuccess && json.data) {
                const authUser: AuthUser = {
                    token: json.data.token,
                    username: json.data.username,
                    role: json.data.role,
                    roleName: json.data.roleName
                };
                setUser(authUser);
                localStorage.setItem('fraudguard_user', JSON.stringify(authUser));
                return true;
            }
            return false;
        } catch {
            return false;
        }
    };

    const register = async (username: string, email: string, password: string, role: number): Promise<boolean> => {
        try {
            const res = await fetch('http://localhost:5217/api/Auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, mail: email, password, role })
            });
            const json = await res.json();
            return json.isSuccess;
        } catch {
            return false;
        }
    };

    const logout = () => {
        setUser(null);
        localStorage.removeItem('fraudguard_user');
    };

    return (
        <AuthContext.Provider value={{ user, login, register, logout, isLoggedIn: !!user }}>
            {children}
        </AuthContext.Provider>
    );
};
