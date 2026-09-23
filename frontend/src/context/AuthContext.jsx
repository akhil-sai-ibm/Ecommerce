import { createContext, useState, useEffect, useContext } from 'react';
import { jwtDecode } from 'jwt-decode';
import api from '../services/api';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const initAuth = () => {
            const token = localStorage.getItem('token');
            if (token) {
                try {
                    const decoded = jwtDecode(token);
                    // Check expiration
                    if (decoded.exp * 1000 < Date.now()) {
                        logout();
                    } else {
                        setUser(parseUser(decoded));
                    }
                } catch (error) {
                    console.error("Invalid token format", error);
                    logout();
                }
            }
            setLoading(false);
        };
        
        initAuth();

        const handleUnauthorized = () => {
            logout();
        };

        window.addEventListener('auth:unauthorized', handleUnauthorized);
        
        return () => {
            window.removeEventListener('auth:unauthorized', handleUnauthorized);
        };
    }, []);

    const parseUser = (decodedToken) => {
        // Handle ASP.NET Core identity claims
        const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
        const nameIdentifier = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
        const emailClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';
        
        const roles = decodedToken[roleClaim] || [];
        const roleArray = Array.isArray(roles) ? roles : [roles];
        
        return {
            id: decodedToken[nameIdentifier] || decodedToken.sub,
            email: decodedToken[emailClaim] || decodedToken.email,
            roles: roleArray,
            isAdmin: roleArray.includes('Admin')
        };
    };

    const login = async (email, password) => {
        const response = await api.post('/auth/login', { email, password });
        const { token } = response.data;
        
        if (token) {
            localStorage.setItem('token', token);
            const decoded = jwtDecode(token);
            setUser(parseUser(decoded));
            return true;
        }
        return false;
    };

    const register = async (email, password) => {
        await api.post('/auth/register', { email, password });
        // The backend register endpoint returns a 201 without a token, so user needs to login after
        return true;
    };

    const logout = () => {
        localStorage.removeItem('token');
        setUser(null);
    };

    const value = {
        user,
        isAuthenticated: !!user,
        isAdmin: user?.isAdmin || false,
        loading,
        login,
        register,
        logout
    };

    return (
        <AuthContext.Provider value={value}>
            {!loading && children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error("useAuth must be used within an AuthProvider");
    }
    return context;
};
