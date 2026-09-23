import { useAuth } from '../context/AuthContext';

const Home = () => {
    const { user } = useAuth();

    return (
        <div>
            <h1>Welcome to ECommerce</h1>
            <p style={{ color: 'var(--text-secondary)', marginTop: '1rem' }}>
                This is the public home page.
            </p>
            
            <div className="card" style={{ marginTop: '2rem' }}>
                <h2>Your Profile</h2>
                <div style={{ marginTop: '1rem', background: '#f4f4f4', padding: '1rem', borderRadius: '4px' }}>
                    <pre>
                        {JSON.stringify(user, null, 2)}
                    </pre>
                </div>
            </div>
        </div>
    );
};

export default Home;
